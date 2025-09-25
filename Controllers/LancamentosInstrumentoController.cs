// Controllers/LancamentosInstrumentoController.cs
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Financeiro.Repositorios;
using Financeiro.Models;

namespace Financeiro.Controllers
{
    public class LancamentosInstrumentoController : Controller
    {
        private readonly ILancamentoInstrumentoRepositorio _repo;
        private readonly IInstrumentoRepositorio _instRepo;

        public LancamentosInstrumentoController(
            ILancamentoInstrumentoRepositorio repo,
            IInstrumentoRepositorio instRepo)
        {
            _repo = repo;
            _instRepo = instRepo;
        }

        private static DateTime PrimeiroDia(int ano, int mes) => new DateTime(ano, mes, 1);

        [HttpGet]
        public async Task<IActionResult> Index(int instrumentoId, int? ano, int? mes)
        {
            var hoje = DateTime.Today;
            var y = ano ?? hoje.Year;
            var m = mes ?? hoje.Month;
            var comp = PrimeitoDia(y, m);

            var resumo = await _instRepo.ObterResumoAsync(instrumentoId); // já traz ValorMensalAtual, etc.
            ViewBag.Resumo = resumo;
            ViewBag.Competencia = comp;

            var itens = await _repo.ListarPorInstrumentoAsync(instrumentoId, comp, comp);
            return View(itens);
        }

        // corrige typo
        private static DateTime PrimeitoDia(int ano, int mes) => new DateTime(ano, mes, 1);

        [HttpGet]
        public IActionResult Novo(int instrumentoId, int? ano, int? mes)
        {
            var hoje = DateTime.Today;
            var y = ano ?? hoje.Year;
            var m = mes ?? hoje.Month;

            var vm = new LancamentoInstrumento
            {
                InstrumentoId = instrumentoId,
                Competencia = new DateTime(y, m, 1)
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Salvar(LancamentoInstrumento vm)
        {
            if (vm.InstrumentoId <= 0) ModelState.AddModelError("", "Instrumento invalido.");
            if (vm.Valor <= 0) ModelState.AddModelError(nameof(vm.Valor), "Informe um valor maior que zero.");

            if (!ModelState.IsValid) return View("Novo", vm);

            // normaliza competência pro 1º dia
            vm.Competencia = new DateTime(vm.Competencia.Year, vm.Competencia.Month, 1);

            await _repo.InserirAsync(vm);
            TempData["Sucesso"] = "Lancamento registrado.";
            return RedirectToAction("Index", new { instrumentoId = vm.InstrumentoId, ano = vm.Competencia.Year, mes = vm.Competencia.Month });
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var m = await _repo.ObterPorIdAsync(id);
            if (m is null) return NotFound();
            return View(m);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Atualizar(int id, decimal valor, string? observacao)
        {
            var m = await _repo.ObterPorIdAsync(id);
            if (m is null) return NotFound();

            if (valor <= 0) ModelState.AddModelError(nameof(valor), "Informe um valor maior que zero.");
            if (!ModelState.IsValid) return View("Editar", m);

            await _repo.AtualizarAsync(id, valor, observacao);
            TempData["Sucesso"] = "Lancamento atualizado.";
            return RedirectToAction("Index", new { instrumentoId = m.InstrumentoId, ano = m.Competencia.Year, mes = m.Competencia.Month });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir(int id)
        {
            var m = await _repo.ObterPorIdAsync(id);
            if (m is null) return NotFound();

            await _repo.ExcluirAsync(id);
            TempData["Sucesso"] = "Lancamento excluido.";
            return RedirectToAction("Index", new { instrumentoId = m.InstrumentoId, ano = m.Competencia.Year, mes = m.Competencia.Month });
        }
    }
}