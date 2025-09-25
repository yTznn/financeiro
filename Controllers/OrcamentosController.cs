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

        // --- DEPENDÊNCIAS CORRIGIDAS ---
        private readonly IOrcamentoRepositorio _orcamentoRepo;
        private readonly ILogService _logService;
        private readonly IJustificativaService _justificativaService;

        public OrcamentosController(
            IOrcamentoRepositorio orcamentoRepo,
            ILogService logService,
            IJustificativaService justificativaService)
        {
            _orcamentoRepo = orcamentoRepo;
            _logService = logService;
            _justificativaService = justificativaService;
        }

        /* -------------------- HELPERS -------------------- */

        private static string Sanitize(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            var cleaned = System.Text.RegularExpressions.Regex.Replace(s, @"\p{Cs}|\u200D|\uFE0F|[\u2600-\u27BF]|[\*\!\@\#']", string.Empty, System.Text.RegularExpressions.RegexOptions.CultureInvariant);
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

        /* -------------------- NOVO (CORRIGIDO) -------------------- */
        [HttpGet]
        public IActionResult Novo() // Não precisa ser async agora
        {
            // REMOVIDO: Busca de Instrumentos (ViewBag)
            return View("OrcamentoForm", new OrcamentoViewModel
            {
                Ativo = true,
                VigenciaInicio = DateTime.Today,
                VigenciaFim = DateTime.Today.AddMonths(1) // Sugestão: um mês de vigência por padrão
            });
        }

        /* -------------------- SALVAR (CORRIGIDO) -------------------- */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Salvar(OrcamentoViewModel vm, string detalhamentoJson, string justificativa = null)
        {
            if (!string.IsNullOrEmpty(detalhamentoJson))
                vm.Detalhamento = JsonSerializer.Deserialize<List<OrcamentoDetalheViewModel>>(detalhamentoJson);

            vm.Nome = Sanitize(vm.Nome);
            vm.Observacao = Sanitize(vm.Observacao);

            if (string.IsNullOrWhiteSpace(vm.Nome))
                ModelState.AddModelError(nameof(vm.Nome), "Informe o nome do orçamento.");
            ValidarDatas(vm);

            vm.ValorPrevistoTotal = RecalcularTotal(vm.Detalhamento);

            // REMOVIDO: Toda a lógica de buscar e comparar com o Instrumento.

            if (!ModelState.IsValid)
            {
                // REMOVIDO: ViewBag.TiposDeAcordo
                return View("OrcamentoForm", vm);
            }

            try
            {
                await _orcamentoRepo.InserirAsync(vm);
                await _logService.RegistrarCriacaoAsync("Orcamento", vm, vm.Id);

                if (!string.IsNullOrWhiteSpace(justificativa))
                {
                    // Lógica de ação simplificada, pois não há mais comparação
                    await _justificativaService.RegistrarAsync(
                        "Orcamento",
                        "Inserção de Orçamento", // Ação simplificada
                        vm.Id,
                        Sanitize(justificativa));
                }

                TempData["Sucesso"] = "Orçamento salvo com sucesso!";
                return RedirectToAction("Index");
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                TempData["Erro"] = "Já existe um registro com dados duplicados. Verifique valores únicos.";
            }
            catch (Exception)
            {
                TempData["Erro"] = "Ops, algo deu errado. Tente novamente.";
            }

            // REMOVIDO: ViewBag.TiposDeAcordo
            return View("OrcamentoForm", vm);
        }

        /* -------------------- EDITAR (GET - CORRIGIDO) -------------------- */
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
                // REMOVIDO: TipoAcordoId = orcamentoHeader.TipoAcordoId,
                VigenciaInicio = orcamentoHeader.VigenciaInicio,
                VigenciaFim = orcamentoHeader.VigenciaFim,
                ValorPrevistoTotal = orcamentoHeader.ValorPrevistoTotal,
                Ativo = orcamentoHeader.Ativo,
                Observacao = orcamentoHeader.Observacao,
                Detalhamento = detalhamentoHierarquico
            };

            // REMOVIDO: ViewBag.TiposDeAcordo
            return View("OrcamentoForm", vm);
        }

        /* -------------------- ATUALIZAR (CORRIGIDO) -------------------- */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Atualizar(OrcamentoViewModel vm, string detalhamentoJson, string justificativa = null)
        {
            if (!string.IsNullOrEmpty(detalhamentoJson))
                vm.Detalhamento = JsonSerializer.Deserialize<List<OrcamentoDetalheViewModel>>(detalhamentoJson);

            vm.Nome = Sanitize(vm.Nome);
            vm.Observacao = Sanitize(vm.Observacao);
            ValidarDatas(vm);
            vm.ValorPrevistoTotal = RecalcularTotal(vm.Detalhamento);
            
            // REMOVIDO: Toda a lógica de buscar e comparar com o Instrumento.

            if (!ModelState.IsValid)
            {
                // REMOVIDO: ViewBag.TiposDeAcordo
                return View("OrcamentoForm", vm);
            }

            var existe = await _orcamentoRepo.ObterHeaderPorIdAsync(vm.Id);
            if (existe == null) return NotFound();

            try
            {
                await _orcamentoRepo.AtualizarAsync(vm.Id, vm);
                await _logService.RegistrarEdicaoAsync("Orcamento", existe, vm, vm.Id);

                if (!string.IsNullOrWhiteSpace(justificativa))
                {
                    await _justificativaService.RegistrarAsync(
                        "Orcamento",
                        "Atualização de Orçamento", // Ação simplificada
                        vm.Id,
                        Sanitize(justificativa));
                }

                TempData["Sucesso"] = "Orçamento atualizado com sucesso!";
                return RedirectToAction("Index");
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                TempData["Erro"] = "Já existe um registro com dados duplicados. Verifique valores únicos.";
            }
            catch (Exception)
            {
                TempData["Erro"] = "Ops, algo deu errado. Tente novamente.";
            }

            // REMOVIDO: ViewBag.TiposDeAcordo
            return View("OrcamentoForm", vm);
        }

        /* -------------------- EXCLUIR (SEM ALTERAÇÕES) -------------------- */
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

        /* -------------------- HIERARQUIA DETALHES (SEM ALTERAÇÕES) -------------------- */
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