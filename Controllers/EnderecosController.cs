using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Financeiro.Models.ViewModels;
using Financeiro.Repositorios;

namespace Financeiro.Controllers
{
    public class EnderecosController : Controller
    {
        private readonly IEnderecoRepositorio _repo;

        public EnderecosController(IEnderecoRepositorio repo)
        {
            _repo = repo;
        }

        /* =========================================================
           NOVO  (GET)
           /Enderecos/Novo?pessoaId=123
        ========================================================= */
        [HttpGet]
        public async Task<IActionResult> Novo(int pessoaId)
        {
            // Se já existir endereço, redireciona para edição
            var existente = await _repo.ObterPorPessoaAsync(pessoaId);
            if (existente is not null)
                return RedirectToAction("Editar", new { pessoaId });

            var vm = new EnderecoViewModel { PessoaJuridicaId = pessoaId };
            return View("EnderecoForm", vm);
        }

        /* =========================================================
           SALVAR  (POST)  — insere e vincula
        ========================================================= */
        [HttpPost]
        public async Task<IActionResult> Salvar(EnderecoViewModel vm)
        {
            if (!ModelState.IsValid)
                return View("EnderecoForm", vm);

            await _repo.InserirAsync(vm);
            return RedirectToAction("Index", "PessoasJuridicas");
        }

        /* =========================================================
           EDITAR  (GET)
           /Enderecos/Editar?pessoaId=123
        ========================================================= */
        [HttpGet]
        public async Task<IActionResult> Editar(int pessoaId)
        {
            var endereco = await _repo.ObterPorPessoaAsync(pessoaId);
            if (endereco is null)         // Se não existir, volta para Novo
                return RedirectToAction("Novo", new { pessoaId });

            var vm = new EnderecoViewModel
            {
                Id               = endereco.Id,
                PessoaJuridicaId = pessoaId,
                Logradouro       = endereco.Logradouro,
                Numero           = endereco.Numero,
                Complemento      = endereco.Complemento,
                Cep              = endereco.Cep,
                Bairro           = endereco.Bairro,
                Municipio        = endereco.Municipio,
                Uf               = endereco.Uf
            };

            return View("EnderecoForm", vm);
        }

        /* =========================================================
           ATUALIZAR  (POST)
        ========================================================= */
        [HttpPost]
        public async Task<IActionResult> Atualizar(int id, EnderecoViewModel vm)
        {
            if (id != vm.Id) return BadRequest();

            if (!ModelState.IsValid)
                return View("EnderecoForm", vm);

            await _repo.AtualizarAsync(id, vm);
            return RedirectToAction("Index", "PessoasJuridicas");
        }
    }
}