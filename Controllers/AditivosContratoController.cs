using Microsoft.AspNetCore.Mvc;
using Financeiro.Models.ViewModels;
using Financeiro.Repositorios;
using Financeiro.Servicos;
using System.Threading.Tasks;

namespace Financeiro.Controllers
{
    /// <summary>
    /// Usado para registrar aditivos (novas versões) de um Contrato.
    /// </summary>
    public class AditivosContratoController : Controller
    {
        private readonly IContratoVersaoRepositorio _versaoRepo;
        private readonly IContratoVersaoService _service;

        public AditivosContratoController(
            IContratoVersaoRepositorio versaoRepo,
            IContratoVersaoService service)
        {
            _versaoRepo = versaoRepo;
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> Novo(int contratoId)
        {
            // Busca a versão atual para exibir os dados no formulário
            var versaoAtual = await _versaoRepo.ObterVersaoAtualAsync(contratoId);
            if (versaoAtual == null)
            {
                // Se não houver versão, o serviço criará a original.
                // Podemos redirecionar para uma página de erro ou deixar o serviço lidar com isso.
                // Por enquanto, vamos permitir que o serviço crie a versão 1.
            }
            
            ViewBag.VersaoAtual = versaoAtual;
            var vm = new AditivoContratoViewModel { ContratoId = contratoId };
            
            // Ainda vamos criar esta View no próximo passo
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
            
            TempData["MensagemSucesso"] = "Aditivo do contrato salvo com sucesso!";
            return RedirectToAction("Editar", "Contratos", new { id = vm.ContratoId });
        }
    }
}