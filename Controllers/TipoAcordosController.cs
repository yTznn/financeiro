using Microsoft.AspNetCore.Mvc;
using Financeiro.Models;
using Financeiro.Models.ViewModels;
using Financeiro.Repositorios;
using System.Threading.Tasks;

namespace Financeiro.Controllers
{
    public class TipoAcordosController : Controller
    {
        private readonly ITipoAcordoRepositorio _repo;

        public TipoAcordosController(ITipoAcordoRepositorio repo)
        {
            _repo = repo;
        }

        // LISTAR
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var lista = await _repo.ListarAsync();
            return View(lista);
        }

        // NOVO
        [HttpGet]
        public IActionResult Novo()
        {
            return View("TipoAcordoForm", new TipoAcordoViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Salvar(TipoAcordoViewModel vm)
        {
            if (!ModelState.IsValid)
                return View("TipoAcordoForm", vm);

            await _repo.InserirAsync(vm);
            return RedirectToAction("Index");
        }

        // EDITAR
        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var acordo = await _repo.ObterPorIdAsync(id);
            if (acordo is null) return NotFound();

            var vm = new TipoAcordoViewModel
            {
                Id              = acordo.Id,
                Numero          = acordo.Numero,
                Valor           = acordo.Valor,
                Objeto          = acordo.Objeto,
                DataInicio      = acordo.DataInicio,
                DataFim         = acordo.DataFim,
                Ativo           = acordo.Ativo,
                Observacao      = acordo.Observacao,
                DataAssinatura  = acordo.DataAssinatura
            };

            return View("TipoAcordoForm", vm);
        }

        [HttpPost]
        public async Task<IActionResult> Atualizar(int id, TipoAcordoViewModel vm)
        {
            if (id != vm.Id) return BadRequest();
            if (!ModelState.IsValid) return View("TipoAcordoForm", vm);

            await _repo.AtualizarAsync(id, vm);
            return RedirectToAction("Index");
        }
    }
}