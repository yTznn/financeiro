using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Financeiro.Models.ViewModels;
using Financeiro.Repositorios;
using Financeiro.Servicos;
using Financeiro.Models;
using Microsoft.Extensions.Logging;

namespace Financeiro.Controllers
{
    public class AditivosInstrumentoController : Controller
    {
        private readonly IInstrumentoVersaoRepositorio _versaoRepo;
        private readonly IInstrumentoRepositorio _instrRepo;
        private readonly IInstrumentoVersaoService _service;
        private readonly ILogService _logService;
        private readonly IJustificativaService _justificativaService;
        private readonly ILogger<AditivosInstrumentoController> _logger;

        public AditivosInstrumentoController(
            IInstrumentoVersaoRepositorio versaoRepo,
            IInstrumentoRepositorio instrRepo,
            IInstrumentoVersaoService service,
            ILogService logService,
            IJustificativaService justificativaService,
            ILogger<AditivosInstrumentoController> logger)
        {
            _versaoRepo = versaoRepo;
            _instrRepo = instrRepo;
            _service = service;
            _logService = logService;
            _justificativaService = justificativaService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Novo(int instrumentoId)
        {
            var atual = await _versaoRepo.ObterVersaoAtualAsync(instrumentoId);
            if (atual == null)
            {
                var pai = await _instrRepo.ObterPorIdAsync(instrumentoId);
                if (pai == null) return NotFound("Instrumento não encontrado.");
                ViewBag.VersaoAtual = new { Versao = 1, Valor = pai.Valor, VigenciaInicio = pai.DataInicio, VigenciaFim = (DateTime?)null };
            }
            else
            {
                ViewBag.VersaoAtual = atual;
            }

            var vm = new AditivoViewModel { TipoAcordoId = instrumentoId };
            return View("AditivoForm", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Salvar(AditivoViewModel vm, string justificativa = null)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.VersaoAtual = await _versaoRepo.ObterVersaoAtualAsync(vm.TipoAcordoId);
                return View("AditivoForm", vm);
            }

            // Normaliza o sinal do valor (delta)
            if (vm.NovoValor.HasValue)
            {
                var abs = Math.Abs(vm.NovoValor.Value);
                switch (vm.TipoAditivo)
                {
                    case TipoAditivo.Supressao:
                    case TipoAditivo.PrazoSupressao:
                        vm.NovoValor = -abs;
                        break;
                    case TipoAditivo.Acrescimo:
                    case TipoAditivo.PrazoAcrescimo:
                        vm.NovoValor = abs;
                        break;
                }
            }
            
            // --- LÓGICA DE DATAS FINAL E CORRIGIDA ---
            bool alteraPrazo = vm.TipoAditivo == TipoAditivo.Prazo
                            || vm.TipoAditivo == TipoAditivo.PrazoAcrescimo
                            || vm.TipoAditivo == TipoAditivo.PrazoSupressao;

            if (!alteraPrazo)
            {
                // Para aditivos que só mudam o valor, a vigência não se altera.
                // Para sinalizar ao Service que ele deve ATUALIZAR a versão atual,
                // definimos a data de início do aditivo como a mesma da versão vigente.
                var atual = await _versaoRepo.ObterVersaoAtualAsync(vm.TipoAcordoId);
                if (atual != null)
                {
                    vm.NovaDataInicio = atual.VigenciaInicio;
                    vm.NovaDataFim = atual.VigenciaFim;
                }
                else
                {
                    // Caso seja o primeiro aditivo de um instrumento sem versão, usa a data do instrumento pai.
                    var pai = await _instrRepo.ObterPorIdAsync(vm.TipoAcordoId);
                    vm.NovaDataInicio = pai?.DataInicio;
                    vm.NovaDataFim = pai?.DataFim;
                }
            }
            // Se 'alteraPrazo' for true, as datas virão do formulário e não precisam ser definidas aqui.


            try
            {
                await _service.CriarAditivoAsync(vm);
                await _logService.RegistrarCriacaoAsync("InstrumentoAditivo", vm, vm.TipoAcordoId);

                if (!string.IsNullOrWhiteSpace(justificativa))
                {
                    await _justificativaService.RegistrarAsync(
                        "InstrumentoAditivo", "Criação de aditivo", vm.TipoAcordoId, justificativa);
                }

                TempData["Sucesso"] = "Aditivo criado com sucesso!";
                return RedirectToAction("Editar", "Instrumentos", new { id = vm.TipoAcordoId });
            }
            catch (ArgumentException ex) // Captura exceções de regra de negócio de forma específica
            {
                _logger.LogWarning("Falha de validação ao salvar aditivo: {message}", ex.Message);
                TempData["Erro"] = ex.Message; // Mostra a mensagem de validação para o usuário
                ViewBag.VersaoAtual = await _versaoRepo.ObterVersaoAtualAsync(vm.TipoAcordoId);
                return View("AditivoForm", vm);
            }
            catch (SqlException ex) when (ex.Number == 8152)
            {
                TempData["Erro"] = "Algum campo excedeu o limite permitido. Reduza o texto.";
                ViewBag.VersaoAtual = await _versaoRepo.ObterVersaoAtualAsync(vm.TipoAcordoId);
                return View("AditivoForm", vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha inesperada ao salvar aditivo para o Instrumento ID {InstrumentoId}", vm.TipoAcordoId);
                TempData["Erro"] = "Ops, algo deu errado ao salvar o aditivo.";
                ViewBag.VersaoAtual = await _versaoRepo.ObterVersaoAtualAsync(vm.TipoAcordoId);
                return View("AditivoForm", vm);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Cancelar([FromBody] CancelarAditivoInstrumentoViewModel body)
        {
            if (body == null || body.InstrumentoId <= 0 || string.IsNullOrWhiteSpace(body.Justificativa))
                return BadRequest("Dados inválidos.");

            try
            {
                var (removida, vigente) = await _service.CancelarUltimoAditivoAsync(body.InstrumentoId, body.Versao, body.Justificativa);

                await _logService.RegistrarExclusaoAsync("InstrumentoAditivo", removida, removida.Id);
                await _justificativaService.RegistrarAsync(
                    "InstrumentoAditivo", $"Cancelamento da versão {removida.Versao}", removida.Id, body.Justificativa );
                
                TempData["Sucesso"] = "Último aditivo foi cancelado com sucesso.";
                return Ok();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                return BadRequest("Não foi possível cancelar: há vínculos relacionados.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao cancelar aditivo para o Instrumento ID {InstrumentoId}", body.InstrumentoId);
                return StatusCode(500, "Ops, algo deu errado ao cancelar o aditivo.");
            }
        }
    }
}