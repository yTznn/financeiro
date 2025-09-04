using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

using Financeiro.Models;
using Financeiro.Models.ViewModels;
using Financeiro.Repositorios;
using Financeiro.Servicos;

namespace Financeiro.Controllers
{
    public class OrcamentosController : Controller
    {
        private static readonly DateTime MinAppDate = new DateTime(2020, 1, 1);

        private readonly IOrcamentoRepositorio _orcamentoRepo;
        private readonly IInstrumentoRepositorio _instrumentoRepo;
        private readonly ILogService _logService;
        private readonly IJustificativaService _justificativaService; // ✅

        public OrcamentosController(
            IOrcamentoRepositorio orcamentoRepo,
            IInstrumentoRepositorio instrumentoRepo,
            ILogService logService,
            IJustificativaService justificativaService) // ✅
        {
            _orcamentoRepo = orcamentoRepo;
            _instrumentoRepo = instrumentoRepo;
            _logService = logService;
            _justificativaService = justificativaService; // ✅
        }

        /* -------------------- HELPERS -------------------- */

        // remove emojis e caracteres proibidos (* ! @ # ')
        private static string Sanitize(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;

            var cleaned = System.Text.RegularExpressions.Regex.Replace(
                s,
                @"\p{Cs}|\u200D|\uFE0F|[\u2600-\u27BF]|[\*\!\@\#']",
                string.Empty,
                System.Text.RegularExpressions.RegexOptions.CultureInvariant
            );

            return cleaned.Trim();
        }

        private void ValidarDatas(OrcamentoViewModel vm)
        {
            if (vm.VigenciaInicio < MinAppDate)
                ModelState.AddModelError(nameof(vm.VigenciaInicio), "Data início deve ser a partir de 01/01/2020.");
            if (vm.VigenciaFim < MinAppDate)
                ModelState.AddModelError(nameof(vm.VigenciaFim), "Data fim deve ser a partir de 01/01/2020.");
            if (vm.VigenciaFim < vm.VigenciaInicio)
                ModelState.AddModelError(nameof(vm.VigenciaFim), "Data fim não pode ser anterior à data início.");
        }

        private static decimal RecalcularTotal(List<OrcamentoDetalheViewModel>? itens)
        {
            if (itens == null || itens.Count == 0) return 0m;

            decimal SomaNode(OrcamentoDetalheViewModel n)
            {
                if (n.Filhos != null && n.Filhos.Count > 0)
                    return n.Filhos.Sum(f => SomaNode(f));

                return n.ValorPrevisto;
            }

            return itens.Sum(SomaNode);
        }

        /* -------------------- LISTAR -------------------- */
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var lista = await _orcamentoRepo.ListarAsync();
            return View(lista);
        }

        /* -------------------- NOVO -------------------- */
        [HttpGet]
        public async Task<IActionResult> Novo()
        {
            ViewBag.TiposDeAcordo = await _instrumentoRepo.ListarAsync();
            return View("OrcamentoForm", new OrcamentoViewModel
            {
                Ativo = true,
                VigenciaInicio = DateTime.Today,
                VigenciaFim = DateTime.Today
            });
        }

        /* -------------------- SALVAR -------------------- */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Salvar(OrcamentoViewModel vm, string detalhamentoJson, string justificativa = null)
        {
            // Monta detalhamento vindo do modal
            if (!string.IsNullOrEmpty(detalhamentoJson))
                vm.Detalhamento = JsonSerializer.Deserialize<List<OrcamentoDetalheViewModel>>(detalhamentoJson);

            // Sanitiza campos de texto
            vm.Nome = Sanitize(vm.Nome);
            vm.Observacao = Sanitize(vm.Observacao);

            // Validações básicas
            if (string.IsNullOrWhiteSpace(vm.Nome))
                ModelState.AddModelError(nameof(vm.Nome), "Informe o nome do orçamento.");
            if (vm.TipoAcordoId <= 0)
                ModelState.AddModelError(nameof(vm.TipoAcordoId), "Selecione um Instrumento.");
            ValidarDatas(vm);

            // Recalcula total de forma confiável no servidor
            vm.ValorPrevistoTotal = RecalcularTotal(vm.Detalhamento);

            // Busca Instrumento para comparação (temerário = exceder)
            var instrumento = vm.TipoAcordoId > 0
                ? await _instrumentoRepo.ObterPorIdAsync(vm.TipoAcordoId)
                : null;

            // Apenas alerta (não bloqueia)
            if (instrumento != null && vm.ValorPrevistoTotal > instrumento.Valor)
            {
                justificativa = Sanitize(justificativa);
                TempData["Alerta"] = $"O total do orçamento ({vm.ValorPrevistoTotal:C}) excede o valor do instrumento ({instrumento.Valor:C}). Informe e registre uma justificativa.";
            }

            if (!ModelState.IsValid)
            {
                ViewBag.TiposDeAcordo = await _instrumentoRepo.ListarAsync();
                return View("OrcamentoForm", vm);
            }

            try
            {
                await _orcamentoRepo.InserirAsync(vm);

                // LOG (como no padrão dos Contratos)
                await _logService.RegistrarCriacaoAsync("Orcamento", vm, vm.Id);

                // JUSTIFICATIVA (se veio do front via SweetAlert)
                if (!string.IsNullOrWhiteSpace(justificativa))
                {
                    var acao = (instrumento != null && vm.ValorPrevistoTotal > instrumento.Valor)
                                ? "Inserção de Orçamento (excedente ao instrumento)"
                                : "Inserção de Orçamento";

                    await _justificativaService.RegistrarAsync(
                        "Orcamento",
                        acao,
                        vm.Id,
                        justificativa);
                }

                TempData["Sucesso"] = "Orçamento salvo com sucesso!";
                return RedirectToAction("Index");
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                TempData["Erro"] = "Já existe um registro com dados duplicados. Verifique valores únicos.";
            }
            catch (SqlException ex) when (ex.Number == 8152)
            {
                TempData["Erro"] = "Algum campo excedeu o limite permitido. Reduza o texto.";
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                TempData["Erro"] = "Não foi possível concluir devido a vínculos relacionados.";
            }
            catch (Exception)
            {
                TempData["Erro"] = "Ops, algo deu errado. Tente novamente.";
            }

            ViewBag.TiposDeAcordo = await _instrumentoRepo.ListarAsync();
            return View("OrcamentoForm", vm);
        }

        /* -------------------- EDITAR (GET) -------------------- */
        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var orcamentoHeader = await _orcamentoRepo.ObterHeaderPorIdAsync(id);
            if (orcamentoHeader == null) return NotFound();

            var detalhes = await _orcamentoRepo.ObterDetalhesPorOrcamentoIdAsync(id);
            var detalhamentoHierarquico = ConstruirHierarquia(detalhes.ToList(), null);

            var vm = new OrcamentoViewModel
            {
                Id = orcamentoHeader.Id,
                Nome = orcamentoHeader.Nome,
                TipoAcordoId = orcamentoHeader.TipoAcordoId,
                VigenciaInicio = orcamentoHeader.VigenciaInicio,
                VigenciaFim = orcamentoHeader.VigenciaFim,
                ValorPrevistoTotal = orcamentoHeader.ValorPrevistoTotal,
                Ativo = orcamentoHeader.Ativo,
                Observacao = orcamentoHeader.Observacao,
                Detalhamento = detalhamentoHierarquico
            };

            ViewBag.TiposDeAcordo = await _instrumentoRepo.ListarAsync();
            return View("OrcamentoForm", vm);
        }

        /* -------------------- ATUALIZAR -------------------- */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Atualizar(OrcamentoViewModel vm, string detalhamentoJson, string justificativa = null)
        {
            // Sanitiza e remonta detalhamento
            vm.Nome = Sanitize(vm.Nome);
            vm.Observacao = Sanitize(vm.Observacao);

            if (!string.IsNullOrEmpty(detalhamentoJson))
                vm.Detalhamento = JsonSerializer.Deserialize<List<OrcamentoDetalheViewModel>>(detalhamentoJson);

            ValidarDatas(vm);
            vm.ValorPrevistoTotal = RecalcularTotal(vm.Detalhamento);

            var instrumento = vm.TipoAcordoId > 0
                ? await _instrumentoRepo.ObterPorIdAsync(vm.TipoAcordoId)
                : null;

            // Apenas alerta (não bloqueia)
            if (instrumento != null && vm.ValorPrevistoTotal > instrumento.Valor)
            {
                justificativa = Sanitize(justificativa);
                TempData["Alerta"] = $"O total do orçamento ({vm.ValorPrevistoTotal:C}) excede o valor do instrumento ({instrumento.Valor:C}). Informe e registre uma justificativa.";
            }

            if (!ModelState.IsValid)
            {
                ViewBag.TiposDeAcordo = await _instrumentoRepo.ListarAsync();
                return View("OrcamentoForm", vm);
            }

            var existe = await _orcamentoRepo.ObterHeaderPorIdAsync(vm.Id);
            if (existe == null) return NotFound();

            try
            {
                await _orcamentoRepo.AtualizarAsync(vm.Id, vm);

                // LOG (igual padrão dos Contratos)
                await _logService.RegistrarEdicaoAsync("Orcamento", null, vm, vm.Id);

                // JUSTIFICATIVA (se houver)
                if (!string.IsNullOrWhiteSpace(justificativa))
                {
                    var acao = (instrumento != null && vm.ValorPrevistoTotal > instrumento.Valor)
                                ? "Atualização de Orçamento (excedente ao instrumento)"
                                : "Atualização de Orçamento";

                    await _justificativaService.RegistrarAsync(
                        "Orcamento",
                        acao,
                        vm.Id,
                        justificativa);
                }

                TempData["Sucesso"] = "Orçamento atualizado com sucesso!";
                return RedirectToAction("Index");
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                TempData["Erro"] = "Já existe um registro com dados duplicados. Verifique valores únicos.";
            }
            catch (SqlException ex) when (ex.Number == 8152)
            {
                TempData["Erro"] = "Algum campo excedeu o limite permitido. Reduza o texto.";
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                TempData["Erro"] = "Não foi possível concluir devido a vínculos relacionados.";
            }
            catch (Exception)
            {
                TempData["Erro"] = "Ops, algo deu errado. Tente novamente.";
            }

            ViewBag.TiposDeAcordo = await _instrumentoRepo.ListarAsync();
            return View("OrcamentoForm", vm);
        }

        /* -------------------- EXCLUIR -------------------- */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir(int id, string justificativa)
        {
            if (string.IsNullOrWhiteSpace(justificativa))
            {
                TempData["Erro"] = "A justificativa é obrigatória para excluir o orçamento.";
                return RedirectToAction(nameof(Index));
            }

            var existente = await _orcamentoRepo.ObterHeaderPorIdAsync(id);
            if (existente == null) return NotFound();

            try
            {
                // ✅ registra justificativa primeiro (mesmo padrão do ContratosController)
                await _justificativaService.RegistrarAsync(
                    "Orcamento",
                    "Exclusão de Orçamento",
                    id,
                    Sanitize(justificativa));

                await _orcamentoRepo.ExcluirAsync(id);

                await _logService.RegistrarExclusaoAsync("Orcamento", existente, id);

                TempData["Sucesso"] = "Orçamento excluído com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                TempData["Erro"] = "Não foi possível excluir: há vínculos relacionados.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                TempData["Erro"] = "Ops, algo deu errado. Tente novamente.";
                return RedirectToAction(nameof(Index));
            }
        }

        /* -------------------- HIERARQUIA DETALHES -------------------- */
        private List<OrcamentoDetalheViewModel> ConstruirHierarquia(List<OrcamentoDetalhe> todos, int? parentId)
        {
            return todos
                .Where(i => i.ParentId == parentId)
                .Select(i => new OrcamentoDetalheViewModel
                {
                    Id = i.Id,
                    ParentId = i.ParentId,
                    Nome = i.Nome,
                    ValorPrevisto = i.ValorPrevisto,
                    Filhos = ConstruirHierarquia(todos, i.Id)
                })
                .ToList();
        }
    }
}