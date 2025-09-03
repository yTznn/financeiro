using Microsoft.AspNetCore.Mvc;
using Financeiro.Models.ViewModels;
using Financeiro.Repositorios;
using Financeiro.Servicos;
using System.Threading.Tasks;
using System;

namespace Financeiro.Controllers
{
    /// <summary>
    /// Usado para registrar aditivos (novas versões) de um Contrato.
    /// </summary>
    public class AditivosContratoController : Controller
    {
        private readonly IContratoVersaoRepositorio _versaoRepo;
        private readonly IContratoVersaoService _service;
        private readonly ILogService _logService;
        private readonly IJustificativaService _justificativaService;

        public AditivosContratoController(
            IContratoVersaoRepositorio versaoRepo,
            IContratoVersaoService service,
            ILogService logService,
            IJustificativaService justificativaService)
        {
            _versaoRepo = versaoRepo;
            _service = service;
            _logService = logService;
            _justificativaService = justificativaService;
        }

        [HttpGet]
        public async Task<IActionResult> Novo(int contratoId)
        {
            var versaoAtual = await _versaoRepo.ObterVersaoAtualAsync(contratoId);
            ViewBag.VersaoAtual = versaoAtual;

            var vm = new AditivoContratoViewModel { ContratoId = contratoId };
            return View("AditivoContratoForm", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Salvar(AditivoContratoViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.VersaoAtual = await _versaoRepo.ObterVersaoAtualAsync(vm.ContratoId);
                return View("AditivoContratoForm", vm);
            }

            await _service.CriarAditivoAsync(vm);

            // ✅ log de criação do aditivo de contrato
            await _logService.RegistrarCriacaoAsync(
                "ContratoAditivo",
                vm,
                vm.ContratoId);

            TempData["MensagemSucesso"] = "Aditivo do contrato salvo com sucesso!";
            return RedirectToAction("Editar", "Contratos", new { id = vm.ContratoId });
        }

        /* ========== CANCELAR ÚLTIMO ADITIVO (POST/AJAX) ========== */
        [HttpPost]
        public async Task<IActionResult> CancelarAditivo([FromBody] CancelarAditivoViewModel body)
        {
            try
            {
                var atual = await _versaoRepo.ObterVersaoAtualAsync(body.ContratoId);
                if (atual == null || atual.Versao != body.Versao)
                    return BadRequest("Versão inválida.");

                // Exclui a versão atual (último aditivo)
                await _versaoRepo.ExcluirAsync(atual.Id);

                // Restaura o contrato a partir da versão anterior (sua infra já provê esse método)
                var anterior = await _versaoRepo.ObterVersaoAtualAsync(body.ContratoId);
                if (anterior != null)
                {
                    await _versaoRepo.RestaurarContratoAPartirDaVersaoAsync(anterior);
                }

                // ✅ log da exclusão (cancelamento do aditivo)
                await _logService.RegistrarExclusaoAsync("ContratoAditivo", atual, atual.Id);

                // ✅ justificativa obrigatória no cancelamento
                await _justificativaService.RegistrarAsync(
                    "ContratoAditivo",
                    $"Cancelamento da versão {atual.Versao}",
                    atual.Id,
                    body.Justificativa);

                return Ok(new { message = "Aditivo cancelado com sucesso." });
            }
            catch (Exception)
            {
                // deixe propagar pro seu handler global; se preferir, retorne algo amigável aqui
                throw;
            }
        }
    }
}