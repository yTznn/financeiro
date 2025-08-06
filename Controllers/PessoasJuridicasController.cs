using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Financeiro.Models.ViewModels;
using Financeiro.Repositorios;
using Financeiro.Validacoes;

namespace Financeiro.Controllers
{
    public class PessoasJuridicasController : Controller
    {
        private readonly IPessoaJuridicaRepositorio _repo;
        private readonly PessoaJuridicaValidacoes _validador;

        public PessoasJuridicasController(IPessoaJuridicaRepositorio repo,
                                          PessoaJuridicaValidacoes validador)
        {
            _repo = repo;
            _validador = validador;
        }

        // ---------- NOVO CADASTRO ----------
        [HttpGet]
        public IActionResult Novo()
            => View("PessoaForm", new PessoaJuridicaViewModel());

        [HttpPost]
        public async Task<IActionResult> Salvar(PessoaJuridicaViewModel vm)
        {
            var resultado = _validador.Validar(vm);
            if (!resultado.EhValido)
            {
                foreach (var erro in resultado.Erros)
                    ModelState.AddModelError(string.Empty, erro);

                return View("PessoaForm", vm);
            }

            await _repo.InserirAsync(vm);
            return RedirectToAction("Index");
        }

        // ---------- 🆕  EDITAR ----------
        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var pj = await _repo.ObterPorIdAsync(id);
            if (pj is null) return NotFound();

            var vm = new PessoaJuridicaViewModel
            {
                Id              = pj.Id,
                RazaoSocial     = pj.RazaoSocial,
                NomeFantasia    = pj.NomeFantasia,
                NumeroInscricao = pj.NumeroInscricao,
                Email           = pj.Email,
                Telefone        = pj.Telefone,
                SituacaoAtiva   = pj.SituacaoAtiva
            };

            return View("PessoaForm", vm);   // mesma view para edição
        }

        [HttpPost]
        public async Task<IActionResult> Atualizar(int id, PessoaJuridicaViewModel vm)
        {
            if (id != vm.Id) return BadRequest();

            var resultado = _validador.Validar(vm);
            if (!resultado.EhValido)
            {
                foreach (var erro in resultado.Erros)
                    ModelState.AddModelError(string.Empty, erro);

                return View("PessoaForm", vm);
            }

            await _repo.AtualizarAsync(id, vm);
            return RedirectToAction("Index");
        }

        // ---------- LISTA ----------
        [HttpGet]
        public async Task<IActionResult> Index()
            => View(await _repo.ListarAsync());
    }
}