using Microsoft.AspNetCore.Mvc;
using Financeiro.Repositorios;
using System.Threading.Tasks;
using Financeiro.Models.ViewModels;
using System.Linq;
using Financeiro.Servicos;
using System;

namespace Financeiro.Controllers
{
    public class ContratosController : Controller
    {
        // ... (construtor e outros métodos se mantêm iguais)

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Salvar(ContratoViewModel vm, string justificativa = null)
        {
            RecalcularValorTotalContrato(vm);

            if (await _contratoRepo.VerificarUnicidadeAsync(vm.NumeroContrato, vm.AnoContrato))
            {
                ModelState.AddModelError("NumeroContrato", "Já existe um contrato ativo com este número para o ano selecionado.");
            }

            if (!ModelState.IsValid)
            {
                // ✅ ALTERAÇÃO AQUI: Coletar todos os erros e passá-los para a TempData.
                var todosErros = ModelState.Values.SelectMany(v => v.Errors)
                                                  .Select(e => e.ErrorMessage);
                TempData["Erro"] = string.Join("<br>", todosErros);

                await PrepararViewBagParaFormulario(vm);
                return View("ContratoForm", vm);
            }

            // ... (resto do método Salvar se mantém igual)
            
            await _contratoRepo.InserirAsync(vm);
            await _logService.RegistrarCriacaoAsync("Contrato", vm, vm.Id);

            if (!string.IsNullOrWhiteSpace(justificativa))
            {
                await _justificativaService.RegistrarAsync(
                    "Contrato",
                    "Inserção com múltiplas naturezas",
                    vm.Id,
                    justificativa);
            }

            TempData["Sucesso"] = "Contrato salvo com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Atualizar(ContratoViewModel vm, string justificativa = null)
        {
            RecalcularValorTotalContrato(vm);
            
            if (await _contratoRepo.VerificarUnicidadeAsync(vm.NumeroContrato, vm.AnoContrato, vm.Id))
            {
                ModelState.AddModelError("NumeroContrato", "Já existe um contrato ativo com este número para o ano selecionado.");
            }

            if (!ModelState.IsValid)
            {
                // ✅ ALTERAÇÃO AQUI: Coletar todos os erros e passá-los para a TempData.
                var todosErros = ModelState.Values.SelectMany(v => v.Errors)
                                                  .Select(e => e.ErrorMessage);
                TempData["Erro"] = string.Join("<br>", todosErros);

                await PrepararViewBagParaFormulario(vm);
                return View("ContratoForm", vm);
            }
            
            // ... (resto do método Atualizar se mantém igual)
            
            await _contratoRepo.AtualizarAsync(vm);
            await _logService.RegistrarEdicaoAsync("Contrato", null, vm, vm.Id);

            if (!string.IsNullOrWhiteSpace(justificativa))
            {
                await _justificativaService.RegistrarAsync(
                    "Contrato",
                    "Atualização com múltiplas naturezas",
                    vm.Id,
                    justificativa);
            }

            TempData["Sucesso"] = "Contrato atualizado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        // ... (resto do controller se mantém igual)
        
        private readonly IContratoRepositorio _contratoRepo;
        private readonly IContratoVersaoRepositorio _versaoRepo;
        private readonly ILogService _logService;
        private readonly IJustificativaService _justificativaService;

        public ContratosController(
            IContratoRepositorio contratoRepo,
            IContratoVersaoRepositorio versaoRepo,
            ILogService logService,
            IJustificativaService justificativaService)
        {
            _contratoRepo = contratoRepo;
            _versaoRepo = versaoRepo;
            _logService = logService;
            _justificativaService = justificativaService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int pagina = 1)
        {
            var (itens, totalPaginas) = await _contratoRepo.ListarPaginadoAsync(pagina);

            // Preenche a quantidade de aditivos de cada contrato
            foreach (var item in itens)
            {
                item.QuantidadeAditivos = await _versaoRepo.ContarPorContratoAsync(item.Contrato.Id);
            }

            ViewBag.TotalPaginas = totalPaginas;
            ViewBag.PaginaAtual = pagina;

            return View(itens);
        }

        [HttpGet]
        public async Task<IActionResult> Novo()
        {
            var vm = new ContratoViewModel();
            await PrepararViewBagParaFormulario(vm);
            return View("ContratoForm", vm);
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var vm = await _contratoRepo.ObterParaEdicaoAsync(id);
            if (vm == null) return NotFound();

            await PrepararViewBagParaFormulario(vm);
            return View("ContratoForm", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir(int id)
        {
            await _contratoRepo.ExcluirAsync(id);
            await _logService.RegistrarExclusaoAsync("Contrato", null, id);

            TempData["Sucesso"] = "Contrato excluído com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        // --- AJAX ---

        [HttpGet]
        public async Task<IActionResult> SugerirNumero(int ano)
        {
            var numero = await _contratoRepo.SugerirProximoNumeroAsync(ano);
            return Json(new { proximoNumero = numero });
        }

        [HttpGet]
        public async Task<IActionResult> BuscarFornecedores(string term = "", int page = 1)
        {
            var (itens, temMais) = await _contratoRepo.BuscarFornecedoresPaginadoAsync(term, page);

            var resultado = new
            {
                results = itens.Select(f => new { id = $"{f.Tipo}-{f.FornecedorId}", text = $"{f.Nome} ({f.Documento})" }),
                pagination = new { more = temMais }
            };
            return Json(resultado);
        }

        [HttpGet]
        public async Task<IActionResult> Historico(int id, int pag = 1)
        {
            var (itens, totalPaginas) = await _versaoRepo.ListarPaginadoAsync(id, pag);
            ViewBag.TotalPaginas = totalPaginas;
            ViewBag.PaginaAtual = pag;
            ViewBag.ContratoId = id;

            return PartialView("_HistoricoContrato", itens);
        }

        private async Task PrepararViewBagParaFormulario(ContratoViewModel vm)
        {
            ViewBag.Naturezas = await _contratoRepo.ListarTodasNaturezasAsync();

            if (!string.IsNullOrEmpty(vm.FornecedorIdCompleto))
            {
                ViewBag.FornecedorAtual = await _contratoRepo.ObterFornecedorPorIdCompletoAsync(vm.FornecedorIdCompleto);
            }
        }
        
        private void RecalcularValorTotalContrato(ContratoViewModel vm)
        {
            if (vm.DataInicio > vm.DataFim)
            {
                vm.ValorContrato = vm.ValorMensal;
                return;
            }

            int numeroDeMeses = ((vm.DataFim.Year - vm.DataInicio.Year) * 12) + vm.DataFim.Month - vm.DataInicio.Month + 1;
            
            if (numeroDeMeses <= 0)
            {
                numeroDeMeses = 1;
            }

            vm.ValorContrato = vm.ValorMensal * numeroDeMeses;
        }
    }
}