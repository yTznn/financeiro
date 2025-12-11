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
    public class PessoasJuridicasController : Controller
    {
        private readonly IPessoaJuridicaRepositorio _repo;
        private readonly IEnderecoRepositorio _endRepo;
        private readonly IContaBancariaRepositorio _contaRepo;
        private readonly PessoaJuridicaValidacoes _validador;
        private readonly ILogService _logService;

        public PessoasJuridicasController(
            IPessoaJuridicaRepositorio repo,
            PessoaJuridicaValidacoes validador,
            IEnderecoRepositorio endRepo,
            IContaBancariaRepositorio contaRepo,
            ILogService logService
        )
        {
            _repo       = repo;
            _validador  = validador;
            _endRepo    = endRepo;
            _contaRepo  = contaRepo;
            _logService = logService;
        }

        /* --- REDIRECIONAMENTO DE SEGURANÇA --- */
        [HttpGet]
        public IActionResult Index()
        {
            // Se tentarem acessar /PessoasJuridicas/Index, joga para a lista unificada
            return RedirectToAction("Index", "Fornecedores");
        }
        /* ------------------------------------- */

        [HttpGet]
        [AutorizarPermissao("FORNECEDOR_ADD")]
        public IActionResult Novo()
            => View("PessoaForm", new PessoaJuridicaViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AutorizarPermissao("FORNECEDOR_ADD")]
        public async Task<IActionResult> Salvar(PessoaJuridicaViewModel vm)
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

                var pjAposCriacao = string.IsNullOrWhiteSpace(vm.NumeroInscricao)
                    ? null
                    : await _repo.ObterPorCnpjAsync(vm.NumeroInscricao);

                await _logService.RegistrarCriacaoAsync(
                    "PessoaJuridica",
                    ((object)pjAposCriacao) ?? (object)vm,
                    pjAposCriacao?.Id ?? 0
                );

                TempData["Sucesso"] = "Pessoa Jurídica criada com sucesso!";
                return RedirectToAction("Index", "Fornecedores");
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Não foi possível salvar: {ex.Message}";
                return RedirectToAction("Novo");
            }
        }

        [HttpGet]
        [AutorizarPermissao("FORNECEDOR_EDIT")]
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

            return View("PessoaForm", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AutorizarPermissao("FORNECEDOR_EDIT")]
        public async Task<IActionResult> Atualizar(int id, PessoaJuridicaViewModel vm)
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
                    "PessoaJuridica",
                    antes,
                    ((object)depois) ?? (object)vm,
                    id
                );

                TempData["Sucesso"] = "Pessoa Jurídica atualizada com sucesso!";
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
                var possuiContrato = await _repo.ExisteContratoPorPessoaJuridicaAsync(id);
                if (possuiContrato)
                {
                    TempData["Erro"] = "Não é possível excluir: existem contratos vinculados a esta Pessoa.";
                    return RedirectToAction("Index", "Fornecedores");
                }

                var pjAntes = await _repo.ObterPorIdAsync(id);
                if (pjAntes is null) return NotFound();

                await _repo.ExcluirAsync(id);

                await _logService.RegistrarExclusaoAsync(
                    "PessoaJuridica",
                    pjAntes,
                    id
                );

                TempData["Sucesso"] = "Pessoa Jurídica excluída com sucesso!";
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Não foi possível excluir: {ex.Message}";
            }

            return RedirectToAction("Index", "Fornecedores");
        }
    }
}