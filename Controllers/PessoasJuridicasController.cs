using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Financeiro.Models.ViewModels;
using Financeiro.Repositorios;
using Financeiro.Validacoes;
using Financeiro.Servicos;

namespace Financeiro.Controllers
{
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

        [HttpGet]
        public IActionResult Novo()
            => View("PessoaForm", new PessoaJuridicaViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
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
                // <<< CORRIGIDO: Redireciona para o controller de Fornecedores
                return RedirectToAction("Index", "Fornecedores");
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Não foi possível salvar: {ex.Message}";
                return RedirectToAction("Novo");
            }
        }

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

            return View("PessoaForm", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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
                // <<< CORRIGIDO: Redireciona para o controller de Fornecedores
                return RedirectToAction("Index", "Fornecedores");
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Não foi possível atualizar: {ex.Message}";
                return View("PessoaForm", vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Index(int pagina = 1)
        {
            // Este método Index original não é mais a view principal de listagem.
            // Pode ser removido se não for mais utilizado por nenhuma outra parte do sistema.
            const int itensPorPagina = 3;

            var (pessoas, totalRegistros) = await _repo.ListarPaginadoAsync(pagina, itensPorPagina);
            var lista = new List<PessoaJuridicaListaViewModel>();

            foreach (var p in pessoas)
            {
                var possuiEnd   = await _endRepo.ObterPorPessoaAsync(p.Id) != null;
                var possuiConta = await _contaRepo.ObterPorPessoaJuridicaAsync(p.Id) != null;

                lista.Add(new PessoaJuridicaListaViewModel
                {
                    Pessoa         = p,
                    PossuiEndereco = possuiEnd,
                    PossuiConta    = possuiConta
                });
            }

            ViewBag.PaginaAtual  = pagina;
            ViewBag.TotalPaginas = (int)Math.Ceiling((double)totalRegistros / itensPorPagina);

            return View(lista);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir(int id)
        {
            try
            {
                var possuiContrato = await _repo.ExisteContratoPorPessoaJuridicaAsync(id);
                if (possuiContrato)
                {
                    TempData["Erro"] = "Não é possível excluir: existem contratos vinculados a esta Pessoa.";
                    // <<< CORRIGIDO: Redireciona para o controller de Fornecedores
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

            // <<< CORRIGIDO: Redireciona para o controller de Fornecedores
            return RedirectToAction("Index", "Fornecedores");
        }
    }
}