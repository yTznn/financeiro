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
            var contrato = await _contratoRepo.ObterParaEdicaoAsync(contratoId);
            if (contrato == null) return NotFound("Contrato não encontrado.");

            var versaoAtual = await _versaoRepo.ObterVersaoAtualAsync(contratoId);
            
            ViewBag.VersaoAtual = versaoAtual ?? new ContratoVersao 
            { 
                Versao = 1, 
                ValorContrato = contrato.ValorContrato,
                DataInicio = contrato.DataInicio,
                DataFim = contrato.DataFim,
                ObjetoContrato = contrato.ObjetoContrato
            };

            var vm = new AditivoContratoViewModel 
            { 
                ContratoId = contratoId,
                DataInicioAditivo = DateTime.Today,
                NovaDataInicio = contrato.DataInicio, 
                NovaDataFim = contrato.DataFim,
                NovoObjeto = contrato.ObjetoContrato,
                Itens = contrato.Itens ?? new List<ContratoItemViewModel>()
            };

            return View("AditivoContratoForm", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Salvar(AditivoContratoViewModel vm)
        {
            // 1. Preenchimento Automático de Datas
            if (!vm.DataInicioAditivo.HasValue || !vm.NovaDataFim.HasValue || !vm.NovaDataInicio.HasValue)
            {
                var atual = await _versaoRepo.ObterVersaoAtualAsync(vm.ContratoId);
                if (atual != null)
                {
                    if (!vm.DataInicioAditivo.HasValue) vm.DataInicioAditivo = atual.DataInicio;
                    if (!vm.NovaDataInicio.HasValue) vm.NovaDataInicio = atual.DataInicio;
                    if (!vm.NovaDataFim.HasValue) vm.NovaDataFim = atual.DataFim;
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

            // 2. Validação do Orçamento (Trava Financeira ITEM A ITEM)
            bool alteraValor = vm.TipoAditivo == TipoAditivo.Acrescimo || 
                               vm.TipoAditivo == TipoAditivo.Supressao || 
                               vm.TipoAditivo == TipoAditivo.PrazoAcrescimo || 
                               vm.TipoAditivo == TipoAditivo.PrazoSupressao;

            // Lista para acumular erros e exibir no SweetAlert
            var errosOrcamento = new List<string>();

            if (alteraValor && vm.Itens != null)
            {
                foreach (var item in vm.Itens)
                {
                    var detalheOrcamento = await _orcamentoRepo.ObterDetalhePorIdAsync(item.Id);

                    if (detalheOrcamento != null)
                    {
                        var jaGastoOutros = await _contratoRepo.ObterTotalComprometidoPorDetalheAsync(item.Id, ignorarContratoId: vm.ContratoId);
                        var saldoDisponivelItem = detalheOrcamento.ValorPrevisto - jaGastoOutros;
                        decimal novoValorItem = item.Valor;

                        if (novoValorItem > (saldoDisponivelItem + 0.01m))
                        {
                            string msgErro = $"Saldo insuficiente no item <b>'{detalheOrcamento.Nome}'</b>.<br>" +
                                             $"Disponível: {saldoDisponivelItem:C2}.<br>" +
                                             $"Tentativa Aditivo: {novoValorItem:C2}.";
                            
                            ModelState.AddModelError("", msgErro);
                            errosOrcamento.Add(msgErro); // Adiciona na lista para o TempData
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("", $"Item de orçamento ID {item.Id} não encontrado.");
                    }
                }
            }

            // --- AQUI ESTÁ A MÁGICA: Se tiver erros de orçamento, joga pro TempData pro SweetAlert pegar ---
            if (errosOrcamento.Any())
            {
                TempData["Erro"] = string.Join("<br><br>", errosOrcamento);
            }

            if (!ModelState.IsValid)
            {
                ViewBag.VersaoAtual = await _versaoRepo.ObterVersaoAtualAsync(vm.ContratoId);
                return View("AditivoContratoForm", vm);
            }

            try 
            {
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