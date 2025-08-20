using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Financeiro.Models.ViewModels;
using Financeiro.Repositorios;

namespace Financeiro.Controllers
{
    public class NaturezasController : Controller
    {
        private readonly INaturezaRepositorio _repo;

        public NaturezasController(INaturezaRepositorio repo)
        {
            _repo = repo;
        }

        /* ---------- LISTAR ---------- */
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var lista = await _repo.ListarAsync();
            return View(lista);           
        }

        /* ---------- NOVO ---------- */
        [HttpGet]
        public IActionResult Novo()
            => View("NaturezaForm", new NaturezaViewModel());

        [HttpPost]
        public async Task<IActionResult> Salvar(NaturezaViewModel vm)
        {
            if (!ModelState.IsValid)
                return View("NaturezaForm", vm);

            await _repo.InserirAsync(vm);
            return RedirectToAction("Index");
        }

        /* ---------- EDITAR ---------- */
        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var n = await _repo.ObterPorIdAsync(id);
            if (n is null) return NotFound();

            var vm = new NaturezaViewModel
            {
                Id             = n.Id,
                Nome           = n.Nome,
                NaturezaMedica = n.NaturezaMedica,
                Ativo          = n.Ativo
            };
            return View("NaturezaForm", vm);
        }

        [HttpPost]
        public async Task<IActionResult> Atualizar(int id, NaturezaViewModel vm)
        {
            if (id != vm.Id) return BadRequest();
            if (!ModelState.IsValid) return View("NaturezaForm", vm);

            await _repo.AtualizarAsync(id, vm);
            return RedirectToAction("Index");
        }
    }
}
