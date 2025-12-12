using Microsoft.AspNetCore.Mvc;
using Financeiro.Models.ViewModels;
using Financeiro.Repositorios;
using Financeiro.Servicos;
using System.Threading.Tasks;
using System;
using Financeiro.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging; // Importante para logs de erro

namespace Financeiro.Controllers
{
    [Authorize]
    public class AditivosContratoController : Controller
    {
        private readonly IContratoVersaoRepositorio _versaoRepo;
        private readonly IContratoRepositorio _contratoRepo;
        private readonly IOrcamentoRepositorio _orcamentoRepo;
        private readonly IContratoVersaoService _service;
        private readonly ILogService _logService;
        private readonly IJustificativaService _justificativaService;
        private readonly ILogger<AditivosContratoController> _logger; // Logger injetado

        public AditivosContratoController(
            IContratoVersaoRepositorio versaoRepo,
            IContratoRepositorio contratoRepo,
            IOrcamentoRepositorio orcamentoRepo,
            IContratoVersaoService service,
            ILogService logService,
            IJustificativaService justificativaService,
            ILogger<AditivosContratoController> logger)
        {
            _versaoRepo = versaoRepo;
            _contratoRepo = contratoRepo;
            _orcamentoRepo = orcamentoRepo;
            _service = service;
            _logService = logService;
            _justificativaService = justificativaService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Novo(int contratoId)
        {
            var versaoAtual = await _versaoRepo.ObterVersaoAtualAsync(contratoId);
            
            // Prepara ViewBag para mostrar resumo na tela
            if (versaoAtual == null)
            {
                var contrato = await _contratoRepo.ObterParaEdicaoAsync(contratoId);
                if(contrato != null)
                {
                    ViewBag.VersaoAtual = new ContratoVersao 
                    { 
                        Versao = 1, 
                        ValorContrato = contrato.ValorContrato,
                        DataInicio = contrato.DataInicio,
                        DataFim = contrato.DataFim,
                        ObjetoContrato = contrato.ObjetoContrato
                    };
                }
                else
                {
                    return NotFound("Contrato não encontrado.");
                }
            }
            else 
            {
                ViewBag.VersaoAtual = versaoAtual;
            }

            var vm = new AditivoContratoViewModel { ContratoId = contratoId };
            return View("AditivoContratoForm", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Salvar(AditivoContratoViewModel vm)
        {
            // 1. Preenchimento Automático de Datas (Herança) se não vier preenchido
            if (!vm.DataInicioAditivo.HasValue || !vm.NovaDataFim.HasValue)
            {
                var atual = await _versaoRepo.ObterVersaoAtualAsync(vm.ContratoId);
                if (atual != null)
                {
                    if (!vm.DataInicioAditivo.HasValue) vm.DataInicioAditivo = atual.DataInicio;
                    if (!vm.NovaDataFim.HasValue) vm.NovaDataFim = atual.DataFim;
                }
                else
                {
                    // Fallback para contrato pai
                    var pai = await _contratoRepo.ObterParaEdicaoAsync(vm.ContratoId);
                    if(pai != null)
                    {
                        if (!vm.DataInicioAditivo.HasValue) vm.DataInicioAditivo = pai.DataInicio;
                        if (!vm.NovaDataFim.HasValue) vm.NovaDataFim = pai.DataFim;
                    }
                }
            }

            // 2. Validação do Orçamento (Trava Financeira)
            // Lógica: Se aumentar valor, o saldo do orçamento deve suportar o NOVO TOTAL do contrato.
            if (vm.TipoAditivo == TipoAditivo.Acrescimo || vm.TipoAditivo == TipoAditivo.PrazoAcrescimo)
            {
                var contratoPai = await _contratoRepo.ObterParaEdicaoAsync(vm.ContratoId);
                if (contratoPai != null && contratoPai.OrcamentoId.HasValue)
                {
                    var orcamento = await _orcamentoRepo.ObterHeaderPorIdAsync(contratoPai.OrcamentoId.Value);
                    
                    // Gasto de todos os OUTROS contratos desse orçamento
                    var gastoOutros = await _contratoRepo.ObterTotalComprometidoPorOrcamentoAsync(
                        contratoPai.OrcamentoId.Value, 
                        ignorarContratoId: vm.ContratoId); 
                    
                    var saldoDisponivel = orcamento.ValorPrevistoTotal - gastoOutros;

                    // Calcula quanto vai ficar o contrato
                    decimal valorAtual = contratoPai.ValorContrato; // Ou pegar da última versão se preferir rigor
                    decimal delta = Math.Abs(vm.NovoValorDecimal);

                    if (vm.EhValorMensal)
                    {
                        DateTime iniCalc = vm.DataInicioAditivo ?? DateTime.Today;
                        DateTime fimCalc = vm.NovaDataFim ?? contratoPai.DataFim;
                        int meses = ((fimCalc.Year - iniCalc.Year) * 12) + fimCalc.Month - iniCalc.Month + 1;
                        if (meses < 0) meses = 0;
                        delta = delta * meses;
                    }

                    decimal novoValorTotalContrato = valorAtual + delta;

                    if (novoValorTotalContrato > (saldoDisponivel + 0.01m)) // Margem erro float
                    {
                         TempData["Erro"] = $"Saldo insuficiente no Orçamento. Disponível para este contrato: {saldoDisponivel:C2}. O contrato passaria a custar: {novoValorTotalContrato:C2}.";
                         ViewBag.VersaoAtual = await _versaoRepo.ObterVersaoAtualAsync(vm.ContratoId); // Recarrega viewbag
                         return View("AditivoContratoForm", vm);
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                ViewBag.VersaoAtual = await _versaoRepo.ObterVersaoAtualAsync(vm.ContratoId);
                return View("AditivoContratoForm", vm);
            }

            try 
            {
                await _service.CriarAditivoAsync(vm);
                
                // Log e Justificativa
                await _logService.RegistrarCriacaoAsync("ContratoAditivo", vm, vm.ContratoId);
                await _justificativaService.RegistrarAsync("Contrato", "Aditivo de Contrato", vm.ContratoId, vm.Justificativa);

                TempData["Sucesso"] = "Aditivo registrado com sucesso!";
                return RedirectToAction("Editar", "Contratos", new { id = vm.ContratoId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar aditivo para o contrato {Id}", vm.ContratoId);
                TempData["Erro"] = $"Erro ao salvar aditivo: {ex.Message}";
                ViewBag.VersaoAtual = await _versaoRepo.ObterVersaoAtualAsync(vm.ContratoId);
                return View("AditivoContratoForm", vm);
            }
        }

        // Cancelar Aditivo (Via AJAX, chamado pela View de Contratos)
        [HttpPost]
        public async Task<IActionResult> Cancelar(int contratoId, int versao, string justificativa)
        {
            try
            {
                var (removida, vigente) = await _service.CancelarUltimoAditivoAsync(contratoId, versao, justificativa);
                
                await _justificativaService.RegistrarAsync("Contrato", $"Cancelamento Aditivo V.{versao}", contratoId, justificativa);
                await _logService.RegistrarExclusaoAsync("ContratoAditivo", removida, contratoId);

                return Json(new { sucesso = true, mensagem = "Aditivo cancelado com sucesso." });
            }
            catch (Exception ex)
            {
                return Json(new { sucesso = false, mensagem = ex.Message });
            }
        }
    }
}