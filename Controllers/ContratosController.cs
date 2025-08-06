using Microsoft.AspNetCore.Mvc;
using Financeiro.Repositorios;
using System.Threading.Tasks;
using Financeiro.Models.ViewModels;
using System.Linq;

namespace Financeiro.Controllers
{
    public class ContratosController : Controller
    {
        private readonly IContratoRepositorio _contratoRepo;

        public ContratosController(IContratoRepositorio contratoRepo)
        {
            _contratoRepo = contratoRepo;
        }

        // --- Ações Principais (Telas) ---

        [HttpGet]
        public async Task<IActionResult> Index(int pagina = 1)
        {
            var (itens, totalPaginas) = await _contratoRepo.ListarPaginadoAsync(pagina);
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
            if (vm == null)
            {
                return NotFound();
            }
            // ✅ CORREÇÃO: Passa o ViewModel para o método preparar o ViewBag
            await PrepararViewBagParaFormulario(vm);
            return View("ContratoForm", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Salvar(ContratoViewModel vm)
        {
            if (await _contratoRepo.VerificarUnicidadeAsync(vm.NumeroContrato, vm.AnoContrato))
            {
                ModelState.AddModelError("NumeroContrato", "Já existe um contrato ativo com este número para o ano selecionado.");
            }

            if (!ModelState.IsValid)
            {
                await PrepararViewBagParaFormulario(vm);
                return View("ContratoForm", vm);
            }

            await _contratoRepo.InserirAsync(vm);
            TempData["MensagemSucesso"] = "Contrato salvo com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Atualizar(ContratoViewModel vm)
        {
            if (await _contratoRepo.VerificarUnicidadeAsync(vm.NumeroContrato, vm.AnoContrato, vm.Id))
            {
                ModelState.AddModelError("NumeroContrato", "Já existe um contrato ativo com este número para o ano selecionado.");
            }

            if (!ModelState.IsValid)
            {
                await PrepararViewBagParaFormulario(vm);
                return View("ContratoForm", vm);
            }

            await _contratoRepo.AtualizarAsync(vm);
            TempData["MensagemSucesso"] = "Contrato atualizado com sucesso!";
            return RedirectToAction(nameof(Index));
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir(int id)
        {
            await _contratoRepo.ExcluirAsync(id);
            TempData["MensagemSucesso"] = "Contrato excluído com sucesso!";
            return RedirectToAction(nameof(Index));
        }


        // --- Ações para chamadas AJAX (pelo JavaScript) ---

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
            
            var resultado = new {
                results = itens.Select(f => new { id = $"{f.Tipo}-{f.FornecedorId}", text = $"{f.Nome} ({f.Documento})" }),
                pagination = new { more = temMais }
            };

            return Json(resultado);
        }

        // --- Métodos Auxiliares ---

        // ✅ CORREÇÃO: Método agora recebe o ViewModel para buscar o fornecedor atual
        private async Task PrepararViewBagParaFormulario(ContratoViewModel vm)
        {
            ViewBag.Naturezas = await _contratoRepo.ListarTodasNaturezasAsync();

            // Se for uma edição e tiver um fornecedor, busca os dados dele
            if (vm.Id > 0 && !string.IsNullOrEmpty(vm.FornecedorIdCompleto))
            {
                ViewBag.FornecedorAtual = await _contratoRepo.ObterFornecedorPorIdCompletoAsync(vm.FornecedorIdCompleto);
            }
        }
    }
}