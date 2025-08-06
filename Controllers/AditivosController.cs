using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Financeiro.Models.ViewModels;
using Financeiro.Repositorios;
using Financeiro.Servicos;
using Financeiro.Models;          // 👈 adiciona esta linha

namespace Financeiro.Controllers
{
    /// <summary>
    /// Usado para registrar aditivos (novas versões) de um TipoAcordo.
    /// </summary>
    public class AditivosController : Controller
    {
        private readonly IAditivoRepositorio _versaoRepo;
        private readonly ITipoAcordoRepositorio _acordoRepo;
        private readonly IVersaoAcordoService _service;

        public AditivosController(IAditivoRepositorio versaoRepo,
                                  ITipoAcordoRepositorio acordoRepo,
                                  IVersaoAcordoService service)
        {
            _versaoRepo = versaoRepo;
            _acordoRepo = acordoRepo;
            _service = service;
        }

        /* ========== NOVO (GET) ========== */
        [HttpGet]
        public async Task<IActionResult> Novo(int acordoId)
        {
            // 1) Tenta pegar a versão atual
            var atual = await _versaoRepo.ObterVersaoAtualAsync(acordoId);

            // 2) Se não existir, cria a versão ORIGINAL (versão 1)
            if (atual is null)
            {
                var acordo = await _acordoRepo.ObterPorIdAsync(acordoId);
                if (acordo is null) return NotFound("Acordo não encontrado.");

                var versao1 = new AcordoVersao
                {
                    TipoAcordoId = acordo.Id,
                    Versao = 1,
                    VigenciaInicio = acordo.DataInicio,
                    VigenciaFim = null,
                    Valor = acordo.Valor,
                    Objeto = acordo.Objeto,
                    TipoAditivo = null,             // original
                    Observacao = acordo.Observacao,
                    DataAssinatura = acordo.DataAssinatura,
                    DataRegistro = DateTime.Now
                };
                await _versaoRepo.InserirAsync(versao1);
                atual = versao1;
            }

            // 3) Envia versão atual para a view
            ViewBag.VersaoAtual = atual;
            var vm = new AditivoViewModel { TipoAcordoId = acordoId };
            return View("AditivoForm", vm);
        }
        /* ========== SALVAR (POST) ========== */
        [HttpPost]
        public async Task<IActionResult> Salvar(AditivoViewModel vm)
        {
            if (!ModelState.IsValid)
                return View("AditivoForm", vm);

            await _service.CriarAditivoAsync(vm);
            return RedirectToAction("Editar", "TipoAcordos", new { id = vm.TipoAcordoId });
            // ou RedirectToAction("Index", "TipoAcordos") se preferir voltar à lista
        }
    }
}