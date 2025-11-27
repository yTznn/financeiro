using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Financeiro.Models.ViewModels;
using Financeiro.Repositorios;
using Financeiro.Servicos;
using Financeiro.Models;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;

namespace Financeiro.Controllers
{
    [Authorize]
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

                ViewBag.VersaoAtual = new
                {
                    Versao         = 1,
                    Valor          = pai.Valor,
                    VigenciaInicio = pai.DataInicio,
                    VigenciaFim    = (DateTime?)null
                };
            }
            else
            {
                // 🔒 Padroniza o shape para a view (evita dynamic/binder issues)
                ViewBag.VersaoAtual = new
                {
                    Versao         = atual.Versao,
                    Valor          = atual.Valor,
                    VigenciaInicio = atual.VigenciaInicio,
                    VigenciaFim    = atual.VigenciaFim
                };
            }

            var vm = new AditivoInstrumentoViewModel { InstrumentoId = instrumentoId };
            return View("AditivoForm", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Salvar(AditivoInstrumentoViewModel vm, string? justificativa = null)
        {
            if (vm == null || vm.InstrumentoId <= 0)
                return BadRequest("Instrumento inválido.");

            if (!ModelState.IsValid)
            {
                var atual = await _versaoRepo.ObterVersaoAtualAsync(vm.InstrumentoId);
                ViewBag.VersaoAtual = atual == null
                    ? new { Versao = 1, Valor = 0m, VigenciaInicio = (DateTime?)null, VigenciaFim = (DateTime?)null }
                    : new { Versao = atual.Versao, Valor = atual.Valor, VigenciaInicio = (DateTime?)atual.VigenciaInicio, VigenciaFim = atual.VigenciaFim };
                return View("AditivoForm", vm);
            }

            // Normaliza o sinal do delta de valor
            if (vm.NovoValor.HasValue)
            {
                var abs = Math.Abs(vm.NovoValor.Value);
                vm.NovoValor = vm.TipoAditivo switch
                {
                    TipoAditivo.Supressao or TipoAditivo.PrazoSupressao => -abs,
                    TipoAditivo.Acrescimo or TipoAditivo.PrazoAcrescimo => abs,
                    _ => vm.NovoValor
                };
            }

            // --- CORREÇÃO APLICADA AQUI ---
            // PERMITIR DATAS PERSONALIZADAS SEMPRE (DESTRAMENTO DE DATA)
            // Se o usuário informou datas (mesmo em aditivo só de valor), usamos elas. 
            // Se não informou, herdamos da versão vigente (ou do instrumento original).
            
            // 1. Define Data Início
            if (!vm.NovaDataInicio.HasValue)
            {
                var atual = await _versaoRepo.ObterVersaoAtualAsync(vm.InstrumentoId);
                // Se não tem versão atual, pega do pai (instrumento original)
                var pai = await _instrRepo.ObterPorIdAsync(vm.InstrumentoId);
                vm.NovaDataInicio = atual?.VigenciaInicio ?? pai?.DataInicio;
            }

            // 2. Define Data Fim
            if (!vm.NovaDataFim.HasValue)
            {
                var atual = await _versaoRepo.ObterVersaoAtualAsync(vm.InstrumentoId);
                var pai = await _instrRepo.ObterPorIdAsync(vm.InstrumentoId);
                vm.NovaDataFim = atual?.VigenciaFim ?? pai?.DataFim;
            }
            // -----------------------------

            try
            {
                await _service.CriarAditivoAsync(vm);
                await _logService.RegistrarCriacaoAsync("InstrumentoAditivo", vm, vm.InstrumentoId);

                if (!string.IsNullOrWhiteSpace(justificativa))
                {
                    await _justificativaService.RegistrarAsync(
                        "InstrumentoAditivo",
                        "Criação de aditivo",
                        vm.InstrumentoId,
                        justificativa);
                }

                TempData["Sucesso"] = "Aditivo criado com sucesso!";
                return RedirectToAction("Editar", "Instrumentos", new { id = vm.InstrumentoId });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Falha de validação ao salvar aditivo para instrumento {id}", vm.InstrumentoId);
                TempData["Erro"] = ex.Message;
            }
            catch (SqlException ex) when (ex.Number == 8152)
            {
                TempData["Erro"] = "Algum campo excedeu o limite permitido. Reduza o texto.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha inesperada ao salvar aditivo para o Instrumento ID {InstrumentoId}", vm.InstrumentoId);
                TempData["Erro"] = "Ops, algo deu errado ao salvar o aditivo.";
            }

            // Reexibe view com VersaoAtual padronizada em caso de erro
            var v = await _versaoRepo.ObterVersaoAtualAsync(vm.InstrumentoId);
            ViewBag.VersaoAtual = v == null
                ? new { Versao = 1, Valor = 0m, VigenciaInicio = (DateTime?)null, VigenciaFim = (DateTime?)null }
                : new { Versao = v.Versao, Valor = v.Valor, VigenciaInicio = (DateTime?)v.VigenciaInicio, VigenciaFim = v.VigenciaFim };
            return View("AditivoForm", vm);
        }

        [HttpPost]
        public async Task<IActionResult> Cancelar([FromBody] CancelarAditivoInstrumentoViewModel body)
        {
            if (body == null || body.InstrumentoId <= 0 || string.IsNullOrWhiteSpace(body.Justificativa))
                return BadRequest("Dados inválidos.");

            try
            {
                var (removida, vigente) = await _service.CancelarUltimoAditivoAsync(
                    body.InstrumentoId, body.Versao, body.Justificativa);

                await _logService.RegistrarExclusaoAsync("InstrumentoAditivo", removida, removida.Id);
                await _justificativaService.RegistrarAsync(
                    "InstrumentoAditivo",
                    $"Cancelamento da versão {removida.Versao}",
                    removida.Id,
                    body.Justificativa
                );

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