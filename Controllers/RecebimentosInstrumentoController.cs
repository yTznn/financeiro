using Financeiro.Models.ViewModels;
using Financeiro.Repositorios;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using System.Linq; // Adicionado para LINQ
using Financeiro.Servicos;
using Microsoft.AspNetCore.Authorization;
using Financeiro.Atributos;

namespace Financeiro.Controllers
{
    [Authorize]
    public class RecebimentosInstrumentoController : Controller
    {
        private readonly IRecebimentoInstrumentoRepositorio _recebimentoRepo;
        private readonly IInstrumentoRepositorio _instrumentoRepo;
        private readonly ILogService _logService;

        public RecebimentosInstrumentoController(
            IRecebimentoInstrumentoRepositorio recebimentoRepo,
            IInstrumentoRepositorio instrumentoRepo,
            ILogService logService)
        {
            _recebimentoRepo = recebimentoRepo;
            _instrumentoRepo = instrumentoRepo;
            _logService = logService;
        }

        /* =================== LISTAR (PAGINADO) =================== */
        [HttpGet]
        [AutorizarPermissao("RECEBIMENTO_VIEW")]
        public async Task<IActionResult> Index(int instrumentoId, int p = 1)
        {
            if (instrumentoId == 0)
            {
                TempData["Alerta"] = "Selecione um instrumento para gerenciar os recebimentos.";
                return RedirectToAction("Index", "Instrumentos");
            }

            var instrumento = await _instrumentoRepo.ObterPorIdAsync(instrumentoId);
            if (instrumento == null) return NotFound();

            const int TAMANHO_PAGINA = 10;
            var (lista, total) = await _recebimentoRepo.ListarPaginadoPorInstrumentoAsync(instrumentoId, p, TAMANHO_PAGINA);

            ViewBag.Instrumento = instrumento;
            ViewBag.PaginaAtual = p;
            ViewBag.TotalPaginas = (int)Math.Ceiling((double)total / TAMANHO_PAGINA);

            return View(lista);
        }

        /* =================== NOVO =================== */
        [HttpGet]
        [AutorizarPermissao("RECEBIMENTO_ADD")]
        public async Task<IActionResult> Novo(int instrumentoId)
        {
            var instrumento = await _instrumentoRepo.ObterPorIdAsync(instrumentoId);
            if (instrumento == null) return NotFound();

            var viewModel = new RecebimentoViewModel
            {
                InstrumentoId = instrumentoId,
                InstrumentoNumero = instrumento.Numero,
                DataInicio = DateTime.Today 
            };

            return View("RecebimentoForm", viewModel);
        }

        /* =================== SALVAR =================== */
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AutorizarPermissao("RECEBIMENTO_ADD")]
        public async Task<IActionResult> Salvar(RecebimentoViewModel vm)
        {
            // 1. Ajuste de Datas
            var dataBase = new DateTime(vm.DataInicio.Year, vm.DataInicio.Month, 1);
            vm.DataInicio = dataBase;
            vm.DataFim = dataBase.AddMonths(1).AddDays(-1);

            // 2. Validação Básica (Anti-Erro Silencioso)
            if (!ModelState.IsValid)
            {
                // Captura os erros para mostrar na tela caso o Summary esteja escondido
                var erros = string.Join("<br>", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                TempData["Erro"] = $"Verifique os dados: {erros}";
                
                await PreencherDadosView(vm);
                return View("RecebimentoForm", vm);
            }

            // 3. Validação de VIGÊNCIA (COM A CORREÇÃO DE HORÁRIO .Date)
            var instrumento = await _instrumentoRepo.ObterPorIdAsync(vm.InstrumentoId);
            if (instrumento != null)
            {
                // Usa .Date para ignorar horas/minutos e evitar o bug do "Primeiro Dia"
                if (vm.DataInicio.Date < instrumento.DataInicio.Date || vm.DataFim.Date > instrumento.DataFim.Date)
                {
                    TempData["Erro"] = $"A competência informada ({vm.DataInicio:MM/yyyy}) está fora da vigência do instrumento ({instrumento.DataInicio:dd/MM/yyyy} a {instrumento.DataFim:dd/MM/yyyy}).";
                    
                    vm.InstrumentoNumero = instrumento.Numero;
                    return View("RecebimentoForm", vm);
                }
            }

            try
            {
                var novoId = await _recebimentoRepo.InserirAsync(vm);
                vm.Id = novoId;

                await _logService.RegistrarCriacaoAsync("RecebimentoInstrumento", vm, novoId);
                TempData["Sucesso"] = "Recebimento lançado com sucesso!";
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao salvar: {ex.Message}";
            }
            
            return RedirectToAction("Index", new { instrumentoId = vm.InstrumentoId });
        }

        /* =================== EDITAR =================== */
        [HttpGet]
        [AutorizarPermissao("RECEBIMENTO_EDIT")]
        public async Task<IActionResult> Editar(int id)
        {
            var viewModel = await _recebimentoRepo.ObterParaEdicaoAsync(id);
            if (viewModel == null) return NotFound();

            var instrumento = await _instrumentoRepo.ObterPorIdAsync(viewModel.InstrumentoId);
            viewModel.InstrumentoNumero = instrumento?.Numero;

            return View("RecebimentoForm", viewModel);
        }

        /* =================== ATUALIZAR =================== */
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AutorizarPermissao("RECEBIMENTO_EDIT")]
        public async Task<IActionResult> Atualizar(RecebimentoViewModel vm)
        {
            var dataBase = new DateTime(vm.DataInicio.Year, vm.DataInicio.Month, 1);
            vm.DataInicio = dataBase;
            vm.DataFim = dataBase.AddMonths(1).AddDays(-1);

            if (!ModelState.IsValid)
            {
                var erros = string.Join("<br>", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                TempData["Erro"] = $"Verifique os dados: {erros}";

                await PreencherDadosView(vm);
                return View("RecebimentoForm", vm);
            }

            // Validação de Vigência (Correção .Date)
            var instrumento = await _instrumentoRepo.ObterPorIdAsync(vm.InstrumentoId);
            if (instrumento != null)
            {
                if (vm.DataInicio.Date < instrumento.DataInicio.Date || vm.DataFim.Date > instrumento.DataFim.Date)
                {
                    TempData["Erro"] = $"A competência informada ({vm.DataInicio:MM/yyyy}) está fora da vigência do instrumento ({instrumento.DataInicio:dd/MM/yyyy} a {instrumento.DataFim:dd/MM/yyyy}).";
                    vm.InstrumentoNumero = instrumento.Numero;
                    return View("RecebimentoForm", vm);
                }
            }

            try
            {
                var antes = await _recebimentoRepo.ObterParaEdicaoAsync(vm.Id);
                await _recebimentoRepo.AtualizarAsync(vm);
                
                await _logService.RegistrarEdicaoAsync("RecebimentoInstrumento", antes, vm, vm.Id);
                TempData["Sucesso"] = "Recebimento atualizado com sucesso!";
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao atualizar: {ex.Message}";
            }
            
            return RedirectToAction("Index", new { instrumentoId = vm.InstrumentoId });
        }

        /* =================== EXCLUIR =================== */
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AutorizarPermissao("RECEBIMENTO_DEL")]
        public async Task<IActionResult> Excluir(int id, int instrumentoId)
        {
            try
            {
                var antes = await _recebimentoRepo.ObterParaEdicaoAsync(id);
                if (antes == null) return NotFound();

                await _recebimentoRepo.ExcluirAsync(id);
                
                await _logService.RegistrarExclusaoAsync("RecebimentoInstrumento", antes, id);
                TempData["Sucesso"] = "Recebimento excluído com sucesso!";
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao excluir: {ex.Message}";
            }
            return RedirectToAction("Index", new { instrumentoId = instrumentoId });
        }

        /* =================== RELATÓRIO =================== */
        [HttpGet("RelatorioGeral")]
        [AutorizarPermissao("RECEBIMENTO_VIEW")]
        public async Task<IActionResult> RelatorioGeral()
        {
            ViewData["Title"] = "Relatório Geral de Recebimentos";
            var todos = await _recebimentoRepo.ListarTodosAsync();
            return View(todos);
        }

        // Helper
        private async Task PreencherDadosView(RecebimentoViewModel vm)
        {
            var inst = await _instrumentoRepo.ObterPorIdAsync(vm.InstrumentoId);
            vm.InstrumentoNumero = inst?.Numero;
        }
    }
}