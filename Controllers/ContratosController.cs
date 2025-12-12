using Microsoft.AspNetCore.Mvc;
using Financeiro.Repositorios;
using System.Threading.Tasks;
using Financeiro.Models.ViewModels;
using System.Linq;
using Financeiro.Servicos;
using System;
using Microsoft.AspNetCore.Authorization;
using Financeiro.Extensions; 
using Financeiro.Atributos;

namespace Financeiro.Controllers
{
    [Authorize]
    public class ContratosController : Controller
    {
        private readonly IContratoRepositorio _contratoRepo;
        private readonly IContratoVersaoRepositorio _versaoRepo;
        private readonly ILogService _logService;
        private readonly IJustificativaService _justificativaService;
        private readonly IOrcamentoRepositorio _orcamentoRepo;
        private readonly IContratoVersaoService _versaoService;

        private const int TAMANHO_PAGINA = 10; 

        public ContratosController(
            IContratoRepositorio contratoRepo,
            IContratoVersaoRepositorio versaoRepo,
            ILogService logService,
            IJustificativaService justificativaService,
            IOrcamentoRepositorio orcamentoRepo,
            IContratoVersaoService versaoService)
        {
            _contratoRepo = contratoRepo;
            _versaoRepo = versaoRepo;
            _logService = logService;
            _justificativaService = justificativaService;
            _orcamentoRepo = orcamentoRepo;
            _versaoService = versaoService;
        }

        [HttpGet]
        [AutorizarPermissao("CONTRATO_VIEW")]
        public async Task<IActionResult> Index(int p = 1)
        {
            int entidadeId = User.ObterEntidadeId();
            if (entidadeId == 0) return RedirectToAction("Login", "Conta");
            if (p < 1) p = 1;

            var (itens, totalPaginas) = await _contratoRepo.ListarPaginadoAsync(entidadeId, p, TAMANHO_PAGINA);
            
            foreach (var item in itens) 
            {
                item.QuantidadeAditivos = await _versaoRepo.ContarPorContratoAsync(item.Contrato.Id);
            }
            
            ViewBag.TotalPaginas = totalPaginas;
            ViewBag.PaginaAtual = p;
            return View(itens);
        }

        [HttpGet]
        [AutorizarPermissao("CONTRATO_ADD")]
        public async Task<IActionResult> Novo()
        {
            var vm = new ContratoViewModel
            {
                Ativo = true,
                DataInicio = DateTime.Today,
                DataFim = DateTime.Today.AddYears(1),
                DataAssinatura = DateTime.Today
            };
            await PrepararViewBagParaFormulario(vm);
            return View("ContratoForm", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AutorizarPermissao("CONTRATO_ADD")]
        public async Task<IActionResult> Salvar(ContratoViewModel vm, string justificativa = null)
        {
            // Lógica de cálculo
            int meses = ((vm.DataFim.Year - vm.DataInicio.Year) * 12) + vm.DataFim.Month - vm.DataInicio.Month + 1;
            if (meses < 1) meses = 1;

            if (vm.ValorMensalDecimal > 0)
            {
                vm.ValorContrato = vm.ValorMensalDecimal * meses;
                
                if (vm.Naturezas != null)
                {
                    foreach (var natureza in vm.Naturezas) natureza.Valor = natureza.Valor * meses;
                }
            }

            int entidadeId = User.ObterEntidadeId();
            if (await _contratoRepo.VerificarUnicidadeAsync(vm.NumeroContrato, vm.AnoContrato, entidadeId))
            {
                ModelState.AddModelError("NumeroContrato", "Já existe um contrato ativo com este número/ano nesta unidade.");
            }

            if (vm.OrcamentoId.HasValue)
            {
                var orcamentoHeader = await _orcamentoRepo.ObterHeaderPorIdAsync(vm.OrcamentoId.Value);
                if (orcamentoHeader != null)
                {
                    var jaGasto = await _contratoRepo.ObterTotalComprometidoPorOrcamentoAsync(vm.OrcamentoId.Value);
                    var saldoOrcamento = orcamentoHeader.ValorPrevistoTotal - jaGasto;

                    if (vm.ValorContrato > (saldoOrcamento + 0.01m))
                    {
                        ModelState.AddModelError("ValorMensal", 
                            $"Saldo insuficiente no Orçamento. Disponível: {saldoOrcamento:C2}. Total do Contrato: {vm.ValorContrato:C2}.");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                ReverterCalculoParaView(vm, meses);
                var erros = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                TempData["Erro"] = "Erros de validação:<br>" + string.Join("<br>", erros);
                await PrepararViewBagParaFormulario(vm);
                return View("ContratoForm", vm);
            }
            
            try 
            {
                await _contratoRepo.InserirAsync(vm);
                await _versaoService.CriarVersaoInicialAsync(vm); // Cria V1
                await _logService.RegistrarCriacaoAsync("Contrato", vm, vm.Id);

                if (!string.IsNullOrWhiteSpace(justificativa))
                {
                    await _justificativaService.RegistrarAsync("Contrato", "Inserção com naturezas", vm.Id, justificativa);
                }

                TempData["Sucesso"] = "Contrato salvo com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ReverterCalculoParaView(vm, meses);
                TempData["Erro"] = $"Erro ao salvar: {ex.Message}";
                await PrepararViewBagParaFormulario(vm);
                return View("ContratoForm", vm);
            }
        }

        [HttpGet]
        [AutorizarPermissao("CONTRATO_EDIT")]
        public async Task<IActionResult> Editar(int id)
        {
            var vm = await _contratoRepo.ObterParaEdicaoAsync(id);
            if (vm == null) return NotFound();
            
            await PrepararViewBagParaFormulario(vm);
            
            var historico = await _versaoRepo.ListarPorContratoAsync(id);
            var versaoAtual = historico.FirstOrDefault();
            var versaoOriginal = historico.LastOrDefault() ?? versaoAtual; 

            ViewBag.VersaoAtual = versaoAtual;
            ViewBag.ValorOriginal = versaoOriginal?.ValorContrato ?? vm.ValorContrato;
            
            return View("ContratoForm", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AutorizarPermissao("CONTRATO_EDIT")]
        public async Task<IActionResult> Atualizar(ContratoViewModel vm, string justificativa = null)
        {
            int meses = ((vm.DataFim.Year - vm.DataInicio.Year) * 12) + vm.DataFim.Month - vm.DataInicio.Month + 1;
            if (meses < 1) meses = 1;

            if (vm.ValorMensalDecimal > 0)
            {
                vm.ValorContrato = vm.ValorMensalDecimal * meses;
                
                if (vm.Naturezas != null)
                {
                    foreach (var natureza in vm.Naturezas) natureza.Valor = natureza.Valor * meses;
                }
            }

            int entidadeId = User.ObterEntidadeId();
            if (await _contratoRepo.VerificarUnicidadeAsync(vm.NumeroContrato, vm.AnoContrato, entidadeId, vm.Id))
            {
                ModelState.AddModelError("NumeroContrato", "Já existe um contrato ativo com este número/ano nesta unidade.");
            }

            if (vm.OrcamentoId.HasValue)
            {
                var orcamentoHeader = await _orcamentoRepo.ObterHeaderPorIdAsync(vm.OrcamentoId.Value);
                var jaGastoOutros = await _contratoRepo.ObterTotalComprometidoPorOrcamentoAsync(vm.OrcamentoId.Value, ignorarContratoId: vm.Id);
                var saldoOrcamento = orcamentoHeader.ValorPrevistoTotal - jaGastoOutros;

                if (vm.ValorContrato > (saldoOrcamento + 0.01m))
                {
                    ModelState.AddModelError("ValorMensal", 
                        $"Saldo insuficiente. Disponível: {saldoOrcamento:C2}. Total do Contrato: {vm.ValorContrato:C2}.");
                }
            }

            if (!ModelState.IsValid)
            {
                ReverterCalculoParaView(vm, meses);
                var erros = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                TempData["Erro"] = "Erros de validação:<br>" + string.Join("<br>", erros);
                await PrepararViewBagParaFormulario(vm);
                return View("ContratoForm", vm);
            }
            
            try
            {
                // 1. Atualiza o registro oficial (Vigente)
                await _contratoRepo.AtualizarAsync(vm);
                
                // 2. Sincroniza o histórico da versão atual para refletir esse novo rateio
                await _versaoService.AtualizarSnapshotUltimaVersaoAsync(vm.Id);
                
                await _logService.RegistrarEdicaoAsync("Contrato", null, vm, vm.Id);

                if (!string.IsNullOrWhiteSpace(justificativa))
                {
                    await _justificativaService.RegistrarAsync("Contrato", "Atualização Cadastral/Rateio", vm.Id, justificativa);
                }

                TempData["Sucesso"] = "Dados do contrato atualizados com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ReverterCalculoParaView(vm, meses);
                TempData["Erro"] = $"Erro ao atualizar: {ex.Message}";
                await PrepararViewBagParaFormulario(vm);
                return View("ContratoForm", vm);
            }
        }

        // Cancelar Aditivo
        [HttpPost]
        [AutorizarPermissao("ADITIVO_DEL")]
        public async Task<IActionResult> CancelarUltimoAditivo(int contratoId, int versao, string justificativa)
        {
            try
            {
                var (removida, vigente) = await _versaoService.CancelarUltimoAditivoAsync(contratoId, versao, justificativa);
                
                if (!string.IsNullOrWhiteSpace(justificativa))
                    await _justificativaService.RegistrarAsync("Contrato", $"Cancelamento de Aditivo V.{removida.Versao}", contratoId, justificativa);

                return Json(new { sucesso = true, mensagem = "Último aditivo cancelado com sucesso. O contrato voltou à versão anterior." });
            }
            catch (Exception ex)
            {
                return Json(new { sucesso = false, mensagem = "Erro ao cancelar aditivo: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AutorizarPermissao("CONTRATO_DEL")]
        public async Task<IActionResult> Excluir(int id, string justificativa)
        {
            if (string.IsNullOrWhiteSpace(justificativa)) { TempData["Erro"] = "Justificativa obrigatória."; return RedirectToAction(nameof(Index)); }
            var c = await _contratoRepo.ObterParaEdicaoAsync(id);
            if(c == null) return NotFound();
            
            await _justificativaService.RegistrarAsync("Contrato", "Exclusão", id, justificativa);
            await _contratoRepo.ExcluirAsync(id);
            await _logService.RegistrarExclusaoAsync("Contrato", c, id);
            
            TempData["Sucesso"] = "Excluído com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        // AJAX e Helpers
        [HttpGet]
        public async Task<IActionResult> SugerirNumero(int ano)
        {
            int entidadeId = User.ObterEntidadeId();
            var n = await _contratoRepo.SugerirProximoNumeroAsync(ano, entidadeId);
            return Json(new { proximoNumero = n });
        }
        
        [HttpGet]
        public async Task<IActionResult> BuscarFornecedores(string term = "", int page = 1)
        {
            var (itens, total) = await _contratoRepo.BuscarFornecedoresPaginadoAsync(term, page, 10);
            return Json(new { results = itens.Select(f => new { id = $"{f.Tipo}-{f.FornecedorId}", text = $"{f.Nome} ({f.Documento})" }), pagination = new { more = (page * 10) < total } });
        }
        
        [HttpGet]
        public async Task<IActionResult> Historico(int id, int pag = 1)
        {
            var (itens, total) = await _versaoRepo.ListarPaginadoAsync(id, pag);
            ViewBag.TotalPaginas = total;
            ViewBag.PaginaAtual = pag;
            ViewBag.ContratoId = id;
            return PartialView("_HistoricoContrato", itens);
        }

        private async Task PrepararViewBagParaFormulario(ContratoViewModel vm)
        {
            ViewBag.Naturezas = await _contratoRepo.ListarTodasNaturezasAsync();
            int entidadeId = User.ObterEntidadeId();
            ViewBag.Orcamentos = await _orcamentoRepo.ListarAtivosPorEntidadeAsync(entidadeId); 
            if (!string.IsNullOrEmpty(vm.FornecedorIdCompleto)) ViewBag.FornecedorAtual = await _contratoRepo.ObterFornecedorPorIdCompletoAsync(vm.FornecedorIdCompleto);
        }

        private void ReverterCalculoParaView(ContratoViewModel vm, int meses)
        {
            if (meses > 0 && vm.Naturezas != null && vm.Naturezas.Any())
            {
                foreach (var natureza in vm.Naturezas)
                {
                    if(natureza.Valor != 0) natureza.Valor = natureza.Valor / meses;
                }
            }
        }
    }
}