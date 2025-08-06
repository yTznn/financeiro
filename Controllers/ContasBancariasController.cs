using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Financeiro.Models.ViewModels;
using Financeiro.Repositorios;

namespace Financeiro.Controllers
{
    /// <summary>
    /// Cadastro/edição de contas bancárias para PJ ou PF.
    /// Se a rota vier com ?pf=true, assume Pessoa Física; caso contrário, Pessoa Jurídica.
    /// </summary>
    public class ContasBancariasController : Controller
    {
        private readonly IContaBancariaRepositorio _repo;

        public ContasBancariasController(IContaBancariaRepositorio repo) => _repo = repo;

        /* =========== NOVO (GET) ================================= */
        // /ContasBancarias/Novo?pessoaId=123             ← Pessoa Jurídica
        // /ContasBancarias/Novo?pessoaId=45&pf=true      ← Pessoa Física
        [HttpGet]
        public async Task<IActionResult> Novo(int pessoaId, bool pf = false)
        {
            var existente = pf
                ? await _repo.ObterPorPessoaFisicaAsync(pessoaId)
                : await _repo.ObterPorPessoaJuridicaAsync(pessoaId);

            if (existente != null)
                return RedirectToAction("Editar", new { pessoaId, pf });

            var vm = new ContaBancariaViewModel
            {
                PessoaJuridicaId = pf ? null : pessoaId,
                PessoaFisicaId   = pf ? pessoaId : null
            };

            return View("ContaForm", vm);
        }

        /* =========== SALVAR (POST) ============================== */
        [HttpPost]
        public async Task<IActionResult> Salvar(ContaBancariaViewModel vm, bool pf = false)
        {
            if (!ModelState.IsValid) return View("ContaForm", vm);

            await _repo.InserirAsync(vm);

            return pf
                ? RedirectToAction("Index", "PessoasFisicas")
                : RedirectToAction("Index", "PessoasJuridicas");
        }

        /* =========== EDITAR (GET) =============================== */
        [HttpGet]
        public async Task<IActionResult> Editar(int pessoaId, bool pf = false)
        {
            var conta = pf
                ? await _repo.ObterPorPessoaFisicaAsync(pessoaId)
                : await _repo.ObterPorPessoaJuridicaAsync(pessoaId);

            if (conta == null) return RedirectToAction("Novo", new { pessoaId, pf });

            var vm = new ContaBancariaViewModel
            {
                Id               = conta.Id,
                Banco            = conta.Banco,
                Agencia          = conta.Agencia,
                Conta            = conta.Conta,
                ChavePix         = conta.ChavePix,
                PessoaJuridicaId = pf ? null : pessoaId,
                PessoaFisicaId   = pf ? pessoaId : null
            };

            return View("ContaForm", vm);
        }

        /* =========== ATUALIZAR (POST) =========================== */
        [HttpPost]
        public async Task<IActionResult> Atualizar(int id, ContaBancariaViewModel vm, bool pf = false)
        {
            if (id != vm.Id) return BadRequest();
            if (!ModelState.IsValid) return View("ContaForm", vm);

            await _repo.AtualizarAsync(id, vm);

            return pf
                ? RedirectToAction("Index", "PessoasFisicas")
                : RedirectToAction("Index", "PessoasJuridicas");
        }
    }
}