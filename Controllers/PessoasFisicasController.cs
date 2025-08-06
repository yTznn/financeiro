using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Financeiro.Models.ViewModels;
using Financeiro.Repositorios;
using Financeiro.Validacoes;

namespace Financeiro.Controllers
{
    public class PessoasFisicasController : Controller
    {
        private readonly IPessoaFisicaRepositorio _repo;
        private readonly PessoaFisicaValidacoes _validador;

        public PessoasFisicasController(IPessoaFisicaRepositorio repo,
                                        PessoaFisicaValidacoes validador)
        {
            _repo = repo;
            _validador = validador;
        }

        // Novo
        [HttpGet]
        public IActionResult Novo()
            => View("PessoaForm", new PessoaFisicaViewModel());

        [HttpPost]
        public async Task<IActionResult> Salvar(PessoaFisicaViewModel vm)
        {
            var res = _validador.Validar(vm);
            if (!res.EhValido)
            {
                foreach (var e in res.Erros) ModelState.AddModelError(string.Empty, e);
                return View("PessoaForm", vm);
            }

            await _repo.InserirAsync(vm);
            return RedirectToAction("Index");
        }

        // Editar
        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var pf = await _repo.ObterPorIdAsync(id);
            if (pf is null) return NotFound();

            var vm = new PessoaFisicaViewModel
            {
                Id             = pf.Id,
                Nome           = pf.Nome,
                Sobrenome      = pf.Sobrenome,
                Cpf            = pf.Cpf,
                DataNascimento = pf.DataNascimento,
                Email          = pf.Email,
                Telefone       = pf.Telefone,
                SituacaoAtiva  = pf.SituacaoAtiva
            };

            return View("PessoaForm", vm);
        }

        [HttpPost]
        public async Task<IActionResult> Atualizar(int id, PessoaFisicaViewModel vm)
        {
            if (id != vm.Id) return BadRequest();

            var res = _validador.Validar(vm);
            if (!res.EhValido)
            {
                foreach (var e in res.Erros) ModelState.AddModelError(string.Empty, e);
                return View("PessoaForm", vm);
            }

            await _repo.AtualizarAsync(id, vm);
            return RedirectToAction("Index");
        }

        // Listagem
        [HttpGet]
        public async Task<IActionResult> Index()
            => View(await _repo.ListarAsync());
    }
}