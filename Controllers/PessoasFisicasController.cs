using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Financeiro.Models.ViewModels;
using Financeiro.Repositorios;
using Financeiro.Validacoes;
using Financeiro.Servicos;
using Microsoft.AspNetCore.Authorization;
using Financeiro.Atributos; // Necessário para [AutorizarPermissao]

namespace Financeiro.Controllers
{
    [Authorize]
    public class PessoasFisicasController : Controller
    {
        private readonly IPessoaFisicaRepositorio _repo;
        private readonly IContaBancariaRepositorio _contaRepo;
        private readonly PessoaFisicaValidacoes _validador;
        private readonly ILogService _logService;

        public PessoasFisicasController(
            IPessoaFisicaRepositorio repo,
            PessoaFisicaValidacoes validador,
            IContaBancariaRepositorio contaRepo,
            ILogService logService)
        {
            _repo      = repo;
            _validador = validador;
            _contaRepo = contaRepo;
            _logService = logService;
        }

        /* --- REDIRECIONAMENTO DE SEGURANÇA --- */
        [HttpGet]
        public IActionResult Index()
        {
            // Se tentarem acessar /PessoasFisicas/Index, joga para a lista unificada
            return RedirectToAction("Index", "Fornecedores");
        }
        /* ------------------------------------- */

        [HttpGet]
        [AutorizarPermissao("FORNECEDOR_ADD")]
        public IActionResult Novo()
            => View("PessoaForm", new PessoaFisicaViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AutorizarPermissao("FORNECEDOR_ADD")]
        public async Task<IActionResult> Salvar(PessoaFisicaViewModel vm)
        {
            var res = _validador.Validar(vm);
            if (!res.EhValido)
            {
                foreach (var e in res.Erros) ModelState.AddModelError(string.Empty, e);
                TempData["Erro"] = "Corrija os erros do formulário.";
                return View("PessoaForm", vm);
            }

            try
            {
                await _repo.InserirAsync(vm);

                var pfApos = await _repo.ObterPorCpfAsync(vm.Cpf);

                await _logService.RegistrarCriacaoAsync(
                    "PessoaFisica",
                    (object)pfApos ?? (object)vm,
                    pfApos?.Id ?? 0
                );

                TempData["Sucesso"] = "Pessoa Física criada com sucesso!";
                return RedirectToAction("Index", "Fornecedores");
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("IX_PessoaFisica_CPF"))
                    TempData["Erro"] = "Já existe uma pessoa física cadastrada com este CPF.";
                else
                    TempData["Erro"] = $"Não foi possível salvar: {ex.Message}";

                return RedirectToAction("Novo");
            }
        }

        [HttpGet]
        [AutorizarPermissao("FORNECEDOR_EDIT")]
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
        [ValidateAntiForgeryToken]
        [AutorizarPermissao("FORNECEDOR_EDIT")]
        public async Task<IActionResult> Atualizar(int id, PessoaFisicaViewModel vm)
        {
            if (id != vm.Id) return BadRequest();

            var res = _validador.Validar(vm);
            if (!res.EhValido)
            {
                foreach (var e in res.Erros) ModelState.AddModelError(string.Empty, e);
                TempData["Erro"] = "Corrija os erros do formulário.";
                return View("PessoaForm", vm);
            }

            try
            {
                var antes = await _repo.ObterPorIdAsync(id);
                if (antes is null) return NotFound();

                await _repo.AtualizarAsync(id, vm);

                var depois = await _repo.ObterPorIdAsync(id);

                await _logService.RegistrarEdicaoAsync(
                    "PessoaFisica",
                    antes,
                    (object)depois ?? (object)vm,
                    id
                );

                TempData["Sucesso"] = "Pessoa Física atualizada com sucesso!";
                return RedirectToAction("Index", "Fornecedores");
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Não foi possível atualizar: {ex.Message}";
                return View("PessoaForm", vm);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AutorizarPermissao("FORNECEDOR_DEL")]
        public async Task<IActionResult> Excluir(int id)
        {
            try
            {
                var possuiContrato = await _repo.ExisteContratoPorPessoaFisicaAsync(id);
                if (possuiContrato)
                {
                    TempData["Erro"] = "Não é possível excluir: existem contratos vinculados a esta Pessoa.";
                    return RedirectToAction("Index", "Fornecedores");
                }

                var pfAntes = await _repo.ObterPorIdAsync(id);
                if (pfAntes is null) return NotFound();

                await _repo.ExcluirAsync(id);

                await _logService.RegistrarExclusaoAsync(
                    "PessoaFisica",
                    pfAntes,
                    id
                );

                TempData["Sucesso"] = "Pessoa Física excluída com sucesso!";
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Não foi possível excluir: {ex.Message}";
            }

            return RedirectToAction("Index", "Fornecedores");
        }
    }
}