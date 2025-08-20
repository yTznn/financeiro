using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Financeiro.Models.ViewModels;
using Financeiro.Repositorios;
using Financeiro.Servicos;
using Financeiro.Models;

namespace Financeiro.Controllers
{
    /// <summary>
    /// Usado para registrar e cancelar aditivos (novas versões) de Contrato.
    /// </summary>
    public class AditivosController : Controller
    {
        private readonly IContratoVersaoRepositorio _versaoRepo;
        private readonly IContratoRepositorio       _contratoRepo;
        private readonly IContratoVersaoService     _service;
        private readonly ILogService                _logService;
        private readonly IJustificativaService      _justificativaService;

        public AditivosController(
            IContratoVersaoRepositorio versaoRepo,
            IContratoRepositorio contratoRepo,
            IContratoVersaoService service,
            ILogService logService,
            IJustificativaService justificativaService)
        {
            _versaoRepo           = versaoRepo;
            _contratoRepo         = contratoRepo;
            _service              = service;
            _logService           = logService;
            _justificativaService = justificativaService;
        }

        /* ========== NOVO (GET) ========== */
        [HttpGet]
        public async Task<IActionResult> Novo(int contratoId)
        {
            // Última versão do contrato
            var atual = await _versaoRepo.ObterVersaoAtualAsync(contratoId);
            if (atual is null)
            {
                // Se não houver versão ainda, cria uma original
                var contrato = await _contratoRepo.ObterParaEdicaoAsync(contratoId);
                if (contrato is null) return NotFound("Contrato não encontrado.");

                var versao1 = new ContratoVersao
                {
                    ContratoId     = contratoId,
                    Versao         = 1,
                    DataInicio     = contrato.DataInicio,
                    DataFim        = contrato.DataFim,
                    ValorContrato  = contrato.ValorContrato,
                    ObjetoContrato = contrato.ObjetoContrato,
                    Observacao     = "Versão original do contrato",
                    DataRegistro   = DateTime.Now
                };
                await _versaoRepo.InserirAsync(versao1);
                atual = versao1;
            }

            ViewBag.VersaoAtual = atual;
            var vm = new AditivoContratoViewModel { ContratoId = contratoId };
            return View("AditivoForm", vm);
        }

        /* ========== SALVAR (POST) ========== */
        [HttpPost]
        public async Task<IActionResult> Salvar(AditivoContratoViewModel vm)
        {
            if (!ModelState.IsValid)
                return View("AditivoForm", vm);

            await _service.CriarAditivoAsync(vm);

            // ✅ log de criação
            await _logService.RegistrarCriacaoAsync(
                "ContratoAditivo",
                vm,
                vm.ContratoId);

            TempData["Sucesso"] = "Aditivo criado com sucesso!";
            return RedirectToAction("Editar", "Contratos", new { id = vm.ContratoId });
        }

        /* ========== CANCELAR ÚLTIMO ADITIVO (POST/AJAX) ========== */
        [HttpPost]
        public async Task<IActionResult> CancelarAditivo([FromBody] CancelarAditivoViewModel body)
        {
            try
            {
                // 1
                Console.WriteLine(">>> Iniciando CancelarAditivo");

                var atual = await _versaoRepo.ObterVersaoAtualAsync(body.ContratoId);
                Console.WriteLine(">>> Versão atual: " + (atual?.Versao.ToString() ?? "null"));

                if (atual == null || atual.Versao != body.Versao)
                    return BadRequest("Versão inválida.");

                // 2
                Console.WriteLine(">>> Excluindo versao " + atual.Id);
                await _versaoRepo.ExcluirAsync(atual.Id);

                var anterior = await _versaoRepo.ObterVersaoAtualAsync(body.ContratoId);
                Console.WriteLine(">>> Versao anterior: " + (anterior?.Versao.ToString() ?? "null"));

                if (anterior != null)
                {
                    Console.WriteLine(">>> Restaurando contrato");
                    await _versaoRepo.RestaurarContratoAPartirDaVersaoAsync(anterior);
                }

                Console.WriteLine(">>> Registrando log");
                await _logService.RegistrarExclusaoAsync("ContratoAditivo", atual, atual.Id);

                Console.WriteLine(">>> Registrando justificativa");
                await _justificativaService.RegistrarAsync(
                    "ContratoAditivo",
                    $"Cancelamento da versão {atual.Versao}",
                    atual.Id,
                    body.Justificativa);

                Console.WriteLine(">>> FIM OK");
                return Ok(new { message = "Aditivo cancelado com sucesso." });
            }
            catch(Exception ex)
            {
                Console.WriteLine(">>> ERRO: " + ex.Message);
                throw;
            }
        }
    }
}