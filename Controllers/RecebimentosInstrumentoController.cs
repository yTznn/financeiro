using Financeiro.Models.ViewModels;
using Financeiro.Repositorios;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using Financeiro.Servicos;
using Microsoft.AspNetCore.Authorization; // Adicionar para ILogService

namespace Financeiro.Controllers
{
    [Authorize]
    public class RecebimentosInstrumentoController : Controller
    {
        private readonly IRecebimentoInstrumentoRepositorio _recebimentoRepo;
        private readonly IInstrumentoRepositorio _instrumentoRepo;
        private readonly ILogService _logService; // <-- Dependência adicionada

        public RecebimentosInstrumentoController(
            IRecebimentoInstrumentoRepositorio recebimentoRepo,
            IInstrumentoRepositorio instrumentoRepo,
            ILogService logService) // <-- Dependência adicionada
        {
            _recebimentoRepo = recebimentoRepo;
            _instrumentoRepo = instrumentoRepo;
            _logService = logService; // <-- Dependência adicionada
        }

        [HttpGet]
        public async Task<IActionResult> Index(int instrumentoId)
        {
            if (instrumentoId == 0)
            {
                // Em vez de um erro, redireciona para a lista de instrumentos para o usuário escolher.
                TempData["Alerta"] = "Selecione um instrumento para ver seus recebimentos.";
                return RedirectToAction("Index", "Instrumentos");
            }

            var instrumento = await _instrumentoRepo.ObterPorIdAsync(instrumentoId);
            if (instrumento == null)
            {
                TempData["Erro"] = "Instrumento nao encontrado.";
                return RedirectToAction("Index", "Instrumentos");
            }

            ViewBag.Instrumento = instrumento;
            var listaDeRecebimentos = await _recebimentoRepo.ListarPorInstrumentoAsync(instrumentoId);

            return View(listaDeRecebimentos);
        }

        [HttpGet]
        public async Task<IActionResult> Novo(int instrumentoId)
        {
            var instrumento = await _instrumentoRepo.ObterPorIdAsync(instrumentoId);
            if (instrumento == null)
            {
                TempData["Erro"] = "Instrumento nao encontrado.";
                return RedirectToAction("Index", "Instrumentos");
            }

            var viewModel = new RecebimentoViewModel
            {
                InstrumentoId = instrumentoId,
                InstrumentoNumero = instrumento.Numero
            };

            return View("RecebimentoForm", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Salvar(RecebimentoViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                var instrumento = await _instrumentoRepo.ObterPorIdAsync(vm.InstrumentoId);
                vm.InstrumentoNumero = instrumento?.Numero;
                return View("RecebimentoForm", vm);
            }

            try
            {
                var novoId = await _recebimentoRepo.InserirAsync(vm);
                vm.Id = novoId;

                await _logService.RegistrarCriacaoAsync("RecebimentoInstrumento", vm, novoId);
                TempData["Sucesso"] = "Recebimento lancado com sucesso!";
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Ops, algo deu errado ao salvar: {ex.Message}";
            }
            return RedirectToAction("Index", new { instrumentoId = vm.InstrumentoId });
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var viewModel = await _recebimentoRepo.ObterParaEdicaoAsync(id);
            if (viewModel == null)
            {
                TempData["Erro"] = "Recebimento nao encontrado.";
                return RedirectToAction("Index", "Instrumentos");
            }

            var instrumento = await _instrumentoRepo.ObterPorIdAsync(viewModel.InstrumentoId);
            viewModel.InstrumentoNumero = instrumento?.Numero;

            return View("RecebimentoForm", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Atualizar(RecebimentoViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                var instrumento = await _instrumentoRepo.ObterPorIdAsync(vm.InstrumentoId);
                vm.InstrumentoNumero = instrumento?.Numero;
                return View("RecebimentoForm", vm);
            }

            try
            {
                var antes = await _recebimentoRepo.ObterParaEdicaoAsync(vm.Id);
                if (antes == null) return NotFound();

                await _recebimentoRepo.AtualizarAsync(vm);
                await _logService.RegistrarEdicaoAsync("RecebimentoInstrumento", antes, vm, vm.Id);
                TempData["Sucesso"] = "Recebimento atualizado com sucesso!";
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Ops, algo deu errado ao atualizar: {ex.Message}";
            }
            return RedirectToAction("Index", new { instrumentoId = vm.InstrumentoId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir(int id, int instrumentoId)
        {
            try
            {
                var antes = await _recebimentoRepo.ObterParaEdicaoAsync(id);
                if (antes == null) return NotFound();

                await _recebimentoRepo.ExcluirAsync(id);
                await _logService.RegistrarExclusaoAsync("RecebimentoInstrumento", antes, id);
                TempData["Sucesso"] = "Recebimento excluido com sucesso!";
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Ops, algo deu errado ao excluir: {ex.Message}";
            }
            return RedirectToAction("Index", new { instrumentoId = instrumentoId });
        }

        [HttpGet("RelatorioGeral")] // Rota amigável: /RecebimentosInstrumento/RelatorioGeral
        public async Task<IActionResult> RelatorioGeral()
        {
            ViewData["Title"] = "Relatório Geral de Recebimentos";
            var todosOsRecebimentos = await _recebimentoRepo.ListarTodosAsync();
            return View(todosOsRecebimentos);
        }
    }
}