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
        private readonly IContratoRepositorio _contratoRepo;
        private readonly IContratoVersaoRepositorio _versaoRepo;
        private readonly ILogService _logService;
        private readonly IJustificativaService _justificativaService;
        private readonly IOrcamentoRepositorio _orcamentoRepo;
        private readonly IContratoVersaoService _versaoService;

        // ***** CONSTRUTOR CORRIGIDO *****
        public ContratosController(
            IContratoRepositorio contratoRepo,
            IContratoVersaoRepositorio versaoRepo,
            ILogService logService,
            IJustificativaService justificativaService,
            IOrcamentoRepositorio orcamentoRepo,
            IContratoVersaoService versaoService)
        {
            _contratoRepo = contratoRepo;
            _versaoRepo = versaoRepo;
            _logService = logService;
            _justificativaService = justificativaService;
            _orcamentoRepo = orcamentoRepo;
            _versaoService = versaoService;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Salvar(ContratoViewModel vm, string justificativa = null)
        {
            // Validação de unicidade (número/ano)
            if (await _contratoRepo.VerificarUnicidadeAsync(vm.NumeroContrato, vm.AnoContrato))
            {
                ModelState.AddModelError("NumeroContrato", "Já existe um contrato ativo com este número para o ano selecionado.");
            }

            // Validação geral do model
            if (!ModelState.IsValid)
            {
                var todosErros = ModelState.Values.SelectMany(v => v.Errors)
                                                    .Select(e => e.ErrorMessage);
                TempData["Erro"] = string.Join("<br>", todosErros);

                await PrepararViewBagParaFormulario(vm);
                return View("ContratoForm", vm);
            }
            
            // --- INÍCIO DA LÓGICA DE SALVAMENTO ORQUESTRADA ---

            // 1. Salva o contrato principal no banco. 
            //    O repositório preenche o vm.Id com o ID do novo contrato.
            await _contratoRepo.InserirAsync(vm);

            // 2. Com o ID em mãos, chama o serviço para criar a Versão 1 (original) no histórico.
            await _versaoService.CriarVersaoInicialAsync(vm);

            // 3. Registra o log da criação do contrato.
            await _logService.RegistrarCriacaoAsync("Contrato", vm, vm.Id);

            // 4. Se houver justificativa, registra-a.
            if (!string.IsNullOrWhiteSpace(justificativa))
            {
                await _justificativaService.RegistrarAsync(
                    "Contrato",
                    "Inserção com múltiplas naturezas",
                    vm.Id,
                    justificativa);
            }

            // --- FIM DA LÓGICA ---

            TempData["Sucesso"] = "Contrato salvo com sucesso!";
            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Atualizar(ContratoViewModel vm, string justificativa = null)
        {
            if (await _contratoRepo.VerificarUnicidadeAsync(vm.NumeroContrato, vm.AnoContrato, vm.Id))
            {
                ModelState.AddModelError("NumeroContrato", "Já existe um contrato ativo com este número para o ano selecionado.");
            }

            if (!ModelState.IsValid)
            {
                var todosErros = ModelState.Values.SelectMany(v => v.Errors)
                                                    .Select(e => e.ErrorMessage);
                TempData["Erro"] = string.Join("<br>", todosErros);

                await PrepararViewBagParaFormulario(vm);
                return View("ContratoForm", vm);
            }
            
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

        [HttpGet]
        public async Task<IActionResult> Index(int pagina = 1)
        {
            var (itens, totalPaginas) = await _contratoRepo.ListarPaginadoAsync(pagina);

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
        public async Task<IActionResult> Excluir(int id, string justificativa)
        {
            if (string.IsNullOrWhiteSpace(justificativa))
            {
                TempData["Erro"] = "A justificativa é obrigatória para excluir o contrato.";
                return RedirectToAction(nameof(Index));
            }

            var contratoParaExcluir = await _contratoRepo.ObterParaEdicaoAsync(id);
            if(contratoParaExcluir == null)
            {
                return NotFound();
            }

            await _justificativaService.RegistrarAsync(
                "Contrato",
                "Exclusão de Contrato",
                id,
                justificativa);
            
            await _contratoRepo.ExcluirAsync(id);
            await _logService.RegistrarExclusaoAsync("Contrato", contratoParaExcluir, id);

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
            const int tamanhoPagina = 10;
            var (itens, totalItens) = await _contratoRepo.BuscarFornecedoresPaginadoAsync(term, page, tamanhoPagina);
            bool temMaisPaginas = (page * tamanhoPagina) < totalItens;

            var resultado = new
            {
                results = itens.Select(f => new { id = $"{f.Tipo}-{f.FornecedorId}", text = $"{f.Nome} ({f.Documento})" }),
                pagination = new { more = temMaisPaginas }
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
        
        // ***** MÉTODO HELPER CORRIGIDO E NO LUGAR CERTO *****
        private async Task PrepararViewBagParaFormulario(ContratoViewModel vm)
        {
            ViewBag.Naturezas = await _contratoRepo.ListarTodasNaturezasAsync();
            
            // LINHA QUE BUSCA OS ORÇAMENTOS
            ViewBag.Orcamentos = await _orcamentoRepo.ListarAsync(); 

            if (!string.IsNullOrEmpty(vm.FornecedorIdCompleto))
            {
                ViewBag.FornecedorAtual = await _contratoRepo.ObterFornecedorPorIdCompletoAsync(vm.FornecedorIdCompleto);
            }
        }
    }
}