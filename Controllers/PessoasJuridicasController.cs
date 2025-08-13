using System;
using System.Collections.Generic;
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
        private readonly IEnderecoRepositorio _endRepo;
        private readonly IContaBancariaRepositorio _contaRepo;
        private readonly PessoaJuridicaValidacoes _validador;

        public PessoasJuridicasController(IPessoaJuridicaRepositorio repo,
                                          PessoaJuridicaValidacoes validador,
                                          IEnderecoRepositorio endRepo,
                                          IContaBancariaRepositorio contaRepo)
        {
            _repo      = repo;
            _validador = validador;
            _endRepo   = endRepo;
            _contaRepo = contaRepo;
        }

        /* ======================= NOVO ======================= */
        [HttpGet]
        public IActionResult Novo()
            => View("PessoaForm", new PessoaJuridicaViewModel());

        [HttpPost]
        public async Task<IActionResult> Salvar(PessoaJuridicaViewModel vm)
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

        /* ======================= EDITAR ======================= */
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
        public async Task<IActionResult> Atualizar(int id, PessoaJuridicaViewModel vm)
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

        /* ======================= LISTA COM PAGINAÇÃO ======================= */
        [HttpGet]
        public async Task<IActionResult> Index(int pagina = 1)
        {
            const int itensPorPagina = 3;

            var (pessoas, totalRegistros) = await _repo.ListarPaginadoAsync(pagina, itensPorPagina);
            var lista = new List<PessoaJuridicaListaViewModel>();

            foreach (var p in pessoas)
            {
                var possuiEnd = await _endRepo.ObterPorPessoaAsync(p.Id) != null;
                var possuiConta = await _contaRepo.ObterPorPessoaJuridicaAsync(p.Id) != null;

                lista.Add(new PessoaJuridicaListaViewModel
                {
                    Pessoa = p,
                    PossuiEndereco = possuiEnd,
                    PossuiConta = possuiConta
                });
            }

            ViewBag.PaginaAtual = pagina;
            ViewBag.TotalPaginas = (int)Math.Ceiling((double)totalRegistros / itensPorPagina);

            return View(lista);
        }
    }
}