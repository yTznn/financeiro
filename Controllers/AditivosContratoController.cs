using Microsoft.AspNetCore.Mvc;
using Financeiro.Models.ViewModels;
using Financeiro.Repositorios;
using Financeiro.Servicos;
using System.Threading.Tasks;
using System;
using Financeiro.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

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
        private readonly ILogger<AditivosContratoController> _logger;

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
            // 1. Busca os dados atuais do contrato (incluindo os ITENS vigentes)
            var contrato = await _contratoRepo.ObterParaEdicaoAsync(contratoId);
            if (contrato == null) return NotFound("Contrato não encontrado.");

            var versaoAtual = await _versaoRepo.ObterVersaoAtualAsync(contratoId);
            
            // Prepara ViewBag para mostrar resumo no topo da tela
            ViewBag.VersaoAtual = versaoAtual ?? new ContratoVersao 
            { 
                Versao = 1, 
                ValorContrato = contrato.ValorContrato,
                DataInicio = contrato.DataInicio,
                DataFim = contrato.DataFim,
                ObjetoContrato = contrato.ObjetoContrato
            };

            // 2. Monta o ViewModel carregando os ITENS para a grid e DATAS
            var vm = new AditivoContratoViewModel 
            { 
                ContratoId = contratoId,
                // Data do documento (assinatura do aditivo)
                DataInicioAditivo = DateTime.Today,
                
                // Sugere datas atuais da vigência para facilitar (se o usuário não mexer, mantém)
                NovaDataInicio = contrato.DataInicio, 
                NovaDataFim = contrato.DataFim,
                
                NovoObjeto = contrato.ObjetoContrato,
                
                // CARREGA A GRID: O usuário verá os valores atuais para poder editar
                Itens = contrato.Itens ?? new List<ContratoItemViewModel>()
            };

            return View("AditivoContratoForm", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Salvar(AditivoContratoViewModel vm)
        {
            // 1. Preenchimento Automático de Datas (Herança) se não vier preenchido
            // Isso garante que se o usuário não mexer na data de início, ela não vá nula
            if (!vm.DataInicioAditivo.HasValue || !vm.NovaDataFim.HasValue || !vm.NovaDataInicio.HasValue)
            {
                var atual = await _versaoRepo.ObterVersaoAtualAsync(vm.ContratoId);
                if (atual != null)
                {
                    if (!vm.DataInicioAditivo.HasValue) vm.DataInicioAditivo = atual.DataInicio; // Fallback data doc
                    if (!vm.NovaDataInicio.HasValue) vm.NovaDataInicio = atual.DataInicio;     // Fallback vigência ini
                    if (!vm.NovaDataFim.HasValue) vm.NovaDataFim = atual.DataFim;            // Fallback vigência fim
                }
                else
                {
                    var pai = await _contratoRepo.ObterParaEdicaoAsync(vm.ContratoId);
                    if(pai != null)
                    {
                        if (!vm.DataInicioAditivo.HasValue) vm.DataInicioAditivo = pai.DataInicio;
                        if (!vm.NovaDataInicio.HasValue) vm.NovaDataInicio = pai.DataInicio;
                        if (!vm.NovaDataFim.HasValue) vm.NovaDataFim = pai.DataFim;
                    }
                }
            }

            // 2. Validação do Orçamento (Trava Financeira)
            // Lógica: Se for aditivo de valor (Acréscimo/Supressão), validamos o novo total contra o saldo
            bool alteraValor = vm.TipoAditivo == TipoAditivo.Acrescimo || 
                               vm.TipoAditivo == TipoAditivo.Supressao || 
                               vm.TipoAditivo == TipoAditivo.PrazoAcrescimo || 
                               vm.TipoAditivo == TipoAditivo.PrazoSupressao;

            if (alteraValor)
            {
                var contratoPai = await _contratoRepo.ObterParaEdicaoAsync(vm.ContratoId);
                if (contratoPai != null && contratoPai.OrcamentoId.HasValue)
                {
                    var orcamento = await _orcamentoRepo.ObterHeaderPorIdAsync(contratoPai.OrcamentoId.Value);
                    
                    // Quanto já foi gasto por OUTROS contratos neste mesmo orçamento
                    var gastoOutros = await _contratoRepo.ObterTotalComprometidoPorOrcamentoAsync(
                        contratoPai.OrcamentoId.Value, 
                        ignorarContratoId: vm.ContratoId); 
                    
                    var saldoDisponivel = orcamento.ValorPrevistoTotal - gastoOutros;

                    // O NOVO CUSTO deste contrato será a soma dos itens da grid editada
                    decimal novoValorTotalContrato = 0;
                    if (vm.Itens != null) novoValorTotalContrato = vm.Itens.Sum(x => x.Valor);

                    // Valida se cabe no orçamento
                    // Adicionamos uma margem de 0.01 para evitar erros de arredondamento de float
                    if (novoValorTotalContrato > (saldoDisponivel + 0.01m)) 
                    {
                            TempData["Erro"] = $"Saldo insuficiente no Orçamento. Disponível para este contrato: {saldoDisponivel:C2}. O contrato passaria a custar: {novoValorTotalContrato:C2}.";
                            ViewBag.VersaoAtual = await _versaoRepo.ObterVersaoAtualAsync(vm.ContratoId); 
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
                // O Serviço faz a mágica: Snapshot -> Atualiza Contrato -> Atualiza Itens
                await _service.CriarAditivoAsync(vm);
                
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

        // Cancelar Aditivo (Via AJAX)
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