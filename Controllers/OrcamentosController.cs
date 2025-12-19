using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Financeiro.Models.Dto;
using Financeiro.Models;
using Financeiro.Models.ViewModels;
using Financeiro.Repositorios;
using Financeiro.Servicos;
using Microsoft.AspNetCore.Authorization;
using Financeiro.Atributos; 
using Financeiro.Extensions; 

namespace Financeiro.Controllers
{
    [Authorize]
    public class OrcamentosController : Controller
    {
        private static readonly DateTime MinAppDate = new DateTime(2020, 1, 1);
        private const int TAMANHO_PAGINA = 3; 

        private readonly IOrcamentoRepositorio _orcamentoRepo;
        private readonly IInstrumentoRepositorio _instrumentoRepo; 
        private readonly ILogService _logService;
        private readonly IJustificativaService _justificativaService;

        public OrcamentosController(
            IOrcamentoRepositorio orcamentoRepo,
            IInstrumentoRepositorio instrumentoRepo,
            ILogService logService,
            IJustificativaService justificativaService)
        {
            _orcamentoRepo = orcamentoRepo;
            _instrumentoRepo = instrumentoRepo;
            _logService = logService;
            _justificativaService = justificativaService;
        }

        // ... (Helpers e Index mantidos iguais) ...
        // Vou omitir para economizar espaço, mas eles continuam lá

        /* -------------------- HELPERS -------------------- */
        private async Task CarregarInstrumentos(int? selecionado = null)
        {
            int entidadeId = User.ObterEntidadeId();
            var lista = await _instrumentoRepo.ListarAsync();
            
            ViewBag.Instrumentos = lista
                .Where(i => i.Ativo && i.EntidadeId == entidadeId) 
                .Select(i => new SelectListItem(
                    $"{i.Numero} - {(i.Objeto.Length > 50 ? i.Objeto.Substring(0, 50) + "..." : i.Objeto)}", 
                    i.Id.ToString(), 
                    selecionado == i.Id))
                .ToList();
        }

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

        [HttpGet]
        [AutorizarPermissao("ORCAMENTO_VIEW")]
        public async Task<IActionResult> Index(int p = 1)
        {
            int entidadeId = User.ObterEntidadeId();
            if (entidadeId == 0) return RedirectToAction("Login", "Conta");

            if (p < 1) p = 1;

            var (itens, total) = await _orcamentoRepo.ListarPaginadoAsync(entidadeId, p, TAMANHO_PAGINA);

            ViewBag.PaginaAtual = p;
            ViewBag.TotalPaginas = (int)Math.Ceiling((double)total / TAMANHO_PAGINA);

            return View(itens);
        }

        [HttpGet]
        [AutorizarPermissao("ORCAMENTO_ADD")]
        public async Task<IActionResult> Novo()
        {
            await CarregarInstrumentos(); 
            return View("OrcamentoForm", new OrcamentoViewModel
            {
                Ativo = true,
                VigenciaInicio = DateTime.Today,
                VigenciaFim = DateTime.Today.AddMonths(1)
            });
        }

        /* -------------------- SALVAR (AJUSTADO) -------------------- */
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AutorizarPermissao("ORCAMENTO_ADD")]
        public async Task<IActionResult> Salvar(OrcamentoViewModel vm, string detalhamentoJson, string justificativa = null)
        {
            // --- LIMPEZA DE DATAS (O SEGREDO) ---
            // Forçamos remover o horário antes de qualquer validação ou salvamento
            vm.VigenciaInicio = vm.VigenciaInicio.Date;
            vm.VigenciaFim = vm.VigenciaFim.Date;
            // ------------------------------------

            if (!string.IsNullOrEmpty(detalhamentoJson))
                vm.Detalhamento = JsonSerializer.Deserialize<List<OrcamentoDetalheViewModel>>(detalhamentoJson);

            vm.Nome = Sanitize(vm.Nome);
            vm.Observacao = Sanitize(vm.Observacao);

            if (string.IsNullOrWhiteSpace(vm.Nome))
                ModelState.AddModelError(nameof(vm.Nome), "Informe o nome do orçamento.");
            
            ValidarDatas(vm);
            vm.ValorPrevistoTotal = RecalcularTotal(vm.Detalhamento);

            if (!ModelState.IsValid)
            {
                await CarregarInstrumentos(vm.InstrumentoId);
                return View("OrcamentoForm", vm);
            }

            var instrumentoResumo = await _instrumentoRepo.ObterResumoAsync(vm.InstrumentoId);
            if (instrumentoResumo == null)
            {
                ModelState.AddModelError(nameof(vm.InstrumentoId), "Instrumento inválido ou não encontrado.");
                await CarregarInstrumentos(vm.InstrumentoId);
                return View("OrcamentoForm", vm);
            }

            if (vm.VigenciaInicio < instrumentoResumo.VigenciaInicio || vm.VigenciaFim > instrumentoResumo.VigenciaFimAtual)
            {
                string msg = $"A vigência do orçamento deve estar dentro do prazo do instrumento ({instrumentoResumo.VigenciaInicio:dd/MM/yyyy} a {instrumentoResumo.VigenciaFimAtual:dd/MM/yyyy}).";
                if (vm.VigenciaInicio < instrumentoResumo.VigenciaInicio) ModelState.AddModelError(nameof(vm.VigenciaInicio), msg);
                if (vm.VigenciaFim > instrumentoResumo.VigenciaFimAtual) ModelState.AddModelError(nameof(vm.VigenciaFim), msg);
                await CarregarInstrumentos(vm.InstrumentoId);
                return View("OrcamentoForm", vm);
            }

            var jaComprometido = await _orcamentoRepo.ObterTotalComprometidoPorInstrumentoAsync(vm.InstrumentoId);
            var saldoDisponivel = instrumentoResumo.ValorTotalAtual - jaComprometido;

            if (vm.ValorPrevistoTotal > saldoDisponivel)
            {
                ModelState.AddModelError(nameof(vm.ValorPrevistoTotal), $"Saldo insuficiente no Instrumento. Disponível: {saldoDisponivel:C2}. Tentativa: {vm.ValorPrevistoTotal:C2}.");
                await CarregarInstrumentos(vm.InstrumentoId);
                return View("OrcamentoForm", vm);
            }

            try
            {
                await _orcamentoRepo.InserirAsync(vm);
                await _logService.RegistrarCriacaoAsync("Orcamento", vm, vm.Id);

                if (!string.IsNullOrWhiteSpace(justificativa))
                {
                    await _justificativaService.RegistrarAsync("Orcamento", "Inserção de Orçamento", vm.Id, Sanitize(justificativa));
                }

                TempData["Sucesso"] = "Orçamento salvo com sucesso!";
                return RedirectToAction("Index");
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                TempData["Erro"] = "Já existe um registro com dados duplicados.";
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao salvar: {ex.Message}";
            }

            await CarregarInstrumentos(vm.InstrumentoId);
            return View("OrcamentoForm", vm);
        }

        [HttpGet]
        [AutorizarPermissao("ORCAMENTO_EDIT")]
        public async Task<IActionResult> Editar(int id)
        {
            var orcamentoHeader = await _orcamentoRepo.ObterHeaderPorIdAsync(id);
            if (orcamentoHeader == null) return NotFound();

            var instrumento = await _instrumentoRepo.ObterPorIdAsync(orcamentoHeader.InstrumentoId);
            if (instrumento == null || instrumento.EntidadeId != User.ObterEntidadeId())
            {
                return Forbid();
            }

            var detalhes = await _orcamentoRepo.ObterDetalhesPorOrcamentoIdAsync(id);
            var detalhamentoHierarquico = ConstruirHierarquia(detalhes.ToList(), null);

            var vm = new OrcamentoViewModel
            {
                Id = orcamentoHeader.Id,
                InstrumentoId = orcamentoHeader.InstrumentoId,
                Nome = orcamentoHeader.Nome,
                VigenciaInicio = orcamentoHeader.VigenciaInicio,
                VigenciaFim = orcamentoHeader.VigenciaFim,
                ValorPrevistoTotal = orcamentoHeader.ValorPrevistoTotal,
                Ativo = orcamentoHeader.Ativo,
                Observacao = orcamentoHeader.Observacao,
                Detalhamento = detalhamentoHierarquico
            };

            await CarregarInstrumentos(vm.InstrumentoId);
            return View("OrcamentoForm", vm);
        }

        /* -------------------- ATUALIZAR (AJUSTADO) -------------------- */
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AutorizarPermissao("ORCAMENTO_EDIT")]
        public async Task<IActionResult> Atualizar(OrcamentoViewModel vm, string detalhamentoJson, string justificativa = null)
        {
            // --- LIMPEZA DE DATAS ---
            vm.VigenciaInicio = vm.VigenciaInicio.Date;
            vm.VigenciaFim = vm.VigenciaFim.Date;
            // ------------------------

            if (!string.IsNullOrEmpty(detalhamentoJson))
                vm.Detalhamento = JsonSerializer.Deserialize<List<OrcamentoDetalheViewModel>>(detalhamentoJson);

            vm.Nome = Sanitize(vm.Nome);
            vm.Observacao = Sanitize(vm.Observacao);
            ValidarDatas(vm);
            vm.ValorPrevistoTotal = RecalcularTotal(vm.Detalhamento);

            if (!ModelState.IsValid)
            {
                await CarregarInstrumentos(vm.InstrumentoId);
                return View("OrcamentoForm", vm);
            }

            var existe = await _orcamentoRepo.ObterHeaderPorIdAsync(vm.Id);
            if (existe == null) return NotFound();

            var instrumentoOriginal = await _instrumentoRepo.ObterPorIdAsync(existe.InstrumentoId);
            if (instrumentoOriginal.EntidadeId != User.ObterEntidadeId()) return Forbid();

            if (vm.InstrumentoId != existe.InstrumentoId)
            {
                 var novoInstrumento = await _instrumentoRepo.ObterPorIdAsync(vm.InstrumentoId);
                 if (novoInstrumento.EntidadeId != User.ObterEntidadeId()) return Forbid();
            }

            var instrumentoResumo = await _instrumentoRepo.ObterResumoAsync(vm.InstrumentoId);
            if (instrumentoResumo == null)
            {
                ModelState.AddModelError(nameof(vm.InstrumentoId), "Instrumento inválido.");
                await CarregarInstrumentos(vm.InstrumentoId);
                return View("OrcamentoForm", vm);
            }

            if (vm.VigenciaInicio < instrumentoResumo.VigenciaInicio || vm.VigenciaFim > instrumentoResumo.VigenciaFimAtual)
            {
                string msg = $"A vigência deve estar dentro do prazo do instrumento ({instrumentoResumo.VigenciaInicio:dd/MM/yyyy} a {instrumentoResumo.VigenciaFimAtual:dd/MM/yyyy}).";
                if (vm.VigenciaInicio < instrumentoResumo.VigenciaInicio) ModelState.AddModelError(nameof(vm.VigenciaInicio), msg);
                if (vm.VigenciaFim > instrumentoResumo.VigenciaFimAtual) ModelState.AddModelError(nameof(vm.VigenciaFim), msg);
                await CarregarInstrumentos(vm.InstrumentoId);
                return View("OrcamentoForm", vm);
            }

            var jaComprometidoOutros = await _orcamentoRepo.ObterTotalComprometidoPorInstrumentoAsync(vm.InstrumentoId, ignorarOrcamentoId: vm.Id);
            var saldoDisponivel = instrumentoResumo.ValorTotalAtual - jaComprometidoOutros;

            if (vm.ValorPrevistoTotal > saldoDisponivel)
            {
                ModelState.AddModelError(nameof(vm.ValorPrevistoTotal), $"Saldo insuficiente. Disponível: {saldoDisponivel:C2}. Tentativa: {vm.ValorPrevistoTotal:C2}.");
                await CarregarInstrumentos(vm.InstrumentoId);
                return View("OrcamentoForm", vm);
            }

            try
            {
                await _orcamentoRepo.AtualizarAsync(vm.Id, vm);
                await _logService.RegistrarEdicaoAsync("Orcamento", existe, vm, vm.Id);

                if (!string.IsNullOrWhiteSpace(justificativa))
                {
                    await _justificativaService.RegistrarAsync("Orcamento", "Atualização de Orçamento", vm.Id, Sanitize(justificativa));
                }

                TempData["Sucesso"] = "Orçamento atualizado com sucesso!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao atualizar: {ex.Message}";
            }

            await CarregarInstrumentos(vm.InstrumentoId);
            return View("OrcamentoForm", vm);
        }

        // ... (Excluir e Helpers mantidos) ...
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AutorizarPermissao("ORCAMENTO_DEL")]
        public async Task<IActionResult> Excluir(int id, string justificativa)
        {
            if (string.IsNullOrWhiteSpace(justificativa))
            {
                TempData["Erro"] = "A justificativa é obrigatória para excluir o orçamento.";
                return RedirectToAction(nameof(Index));
            }

            var existente = await _orcamentoRepo.ObterHeaderPorIdAsync(id);
            if (existente == null) return NotFound();

            var instrumento = await _instrumentoRepo.ObterPorIdAsync(existente.InstrumentoId);
            if (instrumento.EntidadeId != User.ObterEntidadeId()) return Forbid();

            try
            {
                await _justificativaService.RegistrarAsync("Orcamento", "Exclusão de Orçamento", id, Sanitize(justificativa));
                await _orcamentoRepo.ExcluirAsync(id);
                await _logService.RegistrarExclusaoAsync("Orcamento", existente, id);

                TempData["Sucesso"] = "Orçamento excluído com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                TempData["Erro"] = "Não foi possível excluir: há contratos vinculados a este orçamento.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao excluir: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

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