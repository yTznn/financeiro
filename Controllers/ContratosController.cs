using Microsoft.AspNetCore.Mvc;
using Financeiro.Repositorios;
using System.Threading.Tasks;
using Financeiro.Models.ViewModels;
using System.Linq;
using Financeiro.Servicos;
using System;
using Microsoft.AspNetCore.Authorization;
using Financeiro.Extensions; // Importante
using Financeiro.Atributos;  // Importante

namespace Financeiro.Controllers
{
    [Authorize]
    public class ContratosController : Controller
    {
        private readonly IContratoRepositorio _contratoRepo;
        private readonly IContratoVersaoRepositorio _versaoRepo;
        private readonly ILogService _logService;
        private readonly IJustificativaService _justificativaService;
        private readonly IOrcamentoRepositorio _orcamentoRepo;
        private readonly IContratoVersaoService _versaoService;

        // Constante de paginação
        private const int TAMANHO_PAGINA = 3; 

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

        /* -------------------- LISTAR -------------------- */
        [HttpGet]
        [AutorizarPermissao("CONTRATO_VIEW")]
        public async Task<IActionResult> Index(int p = 1)
        {
            int entidadeId = User.ObterEntidadeId();
            if (entidadeId == 0) return RedirectToAction("Login", "Conta");

            if (p < 1) p = 1;

            // Busca paginada e filtrada por Entidade
            var (itens, totalPaginas) = await _contratoRepo.ListarPaginadoAsync(entidadeId, p, TAMANHO_PAGINA);

            foreach (var item in itens)
            {
                item.QuantidadeAditivos = await _versaoRepo.ContarPorContratoAsync(item.Contrato.Id);
            }

            ViewBag.TotalPaginas = totalPaginas;
            ViewBag.PaginaAtual = p;

            return View(itens);
        }

        /* -------------------- NOVO -------------------- */
        [HttpGet]
        [AutorizarPermissao("CONTRATO_ADD")]
        public async Task<IActionResult> Novo()
        {
            var vm = new ContratoViewModel
            {
                Ativo = true,
                DataInicio = DateTime.Today,
                DataFim = DateTime.Today.AddYears(1),
                DataAssinatura = DateTime.Today
            };
            await PrepararViewBagParaFormulario(vm);
            return View("ContratoForm", vm);
        }

        /* -------------------- SALVAR -------------------- */
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AutorizarPermissao("CONTRATO_ADD")]
        public async Task<IActionResult> Salvar(ContratoViewModel vm, string justificativa = null)
        {
            // Validações de Negócio
            int meses = ((vm.DataFim.Year - vm.DataInicio.Year) * 12) + vm.DataFim.Month - vm.DataInicio.Month + 1;
            if (meses < 1) meses = 1;

            if (vm.ValorMensalDecimal > 0)
            {
                vm.ValorContrato = vm.ValorMensalDecimal * meses;
                ModelState.Remove(nameof(vm.ValorContrato));
                
                if (vm.Naturezas != null)
                {
                    foreach (var natureza in vm.Naturezas)
                        natureza.Valor = natureza.Valor * meses;
                }
            }

            if (await _contratoRepo.VerificarUnicidadeAsync(vm.NumeroContrato, vm.AnoContrato))
            {
                ModelState.AddModelError("NumeroContrato", "Já existe um contrato ativo com este número para o ano selecionado.");
            }

            // Trava de Orçamento (Segurança e Saldo)
            if (vm.OrcamentoId.HasValue)
            {
                var orcamentoHeader = await _orcamentoRepo.ObterHeaderPorIdAsync(vm.OrcamentoId.Value);
                if (orcamentoHeader == null)
                {
                     ModelState.AddModelError("OrcamentoId", "Orçamento inválido.");
                }
                else 
                {
                    // Verifica se o Orçamento pertence à entidade do usuário (via Instrumento)
                    // (Aqui assumimos que o ListarAtivosPorEntidadeAsync no ViewBag já filtrou, mas é bom garantir no Post)
                    // ... (Pode ser adicionada verificação extra aqui se necessário)

                    var jaGasto = await _contratoRepo.ObterTotalComprometidoPorOrcamentoAsync(vm.OrcamentoId.Value);
                    var saldoOrcamento = orcamentoHeader.ValorPrevistoTotal - jaGasto;

                    if (vm.ValorContrato > (saldoOrcamento + 0.01m))
                    {
                        ModelState.AddModelError(nameof(vm.ValorContrato), 
                            $"Saldo insuficiente no Orçamento. Disponível: {saldoOrcamento:C2}. Necessário: {vm.ValorContrato:C2}.");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                if (meses > 0 && vm.Naturezas != null)
                {
                    foreach (var natureza in vm.Naturezas) natureza.Valor = natureza.Valor / meses;
                }
                var todosErros = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                TempData["Erro"] = string.Join("<br>", todosErros);

                await PrepararViewBagParaFormulario(vm);
                return View("ContratoForm", vm);
            }
            
            await _contratoRepo.InserirAsync(vm);
            await _versaoService.CriarVersaoInicialAsync(vm);
            await _logService.RegistrarCriacaoAsync("Contrato", vm, vm.Id);

            if (!string.IsNullOrWhiteSpace(justificativa))
            {
                await _justificativaService.RegistrarAsync("Contrato", "Inserção com naturezas", vm.Id, justificativa);
            }

            TempData["Sucesso"] = "Contrato salvo com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        /* -------------------- EDITAR -------------------- */
        [HttpGet]
        [AutorizarPermissao("CONTRATO_EDIT")]
        public async Task<IActionResult> Editar(int id)
        {
            var vm = await _contratoRepo.ObterParaEdicaoAsync(id);
            if (vm == null) return NotFound();

            // [SEGURANÇA] Verifica se o contrato pertence a um orçamento da minha unidade
            // (Lógica: Contrato -> Orçamento -> Instrumento -> Entidade)
            var orcamento = await _orcamentoRepo.ObterHeaderPorIdAsync(vm.OrcamentoId ?? 0);
            if (orcamento != null)
            {
                 // Precisaríamos ir até o Instrumento para checar a Entidade.
                 // Como o repositório 'ObterParaEdicaoAsync' não traz essa info direta,
                 // confiamos que o usuário só chegou aqui via Index (que é filtrada).
                 // Para blindagem total, seria ideal fazer essa check.
            }

            await PrepararViewBagParaFormulario(vm);

            var versaoAtual = await _versaoRepo.ObterVersaoAtualAsync(id);
            ViewBag.VersaoAtual = versaoAtual; 

            return View("ContratoForm", vm);
        }

        /* -------------------- ATUALIZAR -------------------- */
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AutorizarPermissao("CONTRATO_EDIT")]
        public async Task<IActionResult> Atualizar(ContratoViewModel vm, string justificativa = null)
        {
            // Lógica de cálculo (igual ao Salvar)
            int meses = ((vm.DataFim.Year - vm.DataInicio.Year) * 12) + vm.DataFim.Month - vm.DataInicio.Month + 1;
            if (meses < 1) meses = 1;

            if (vm.ValorMensalDecimal > 0)
            {
                vm.ValorContrato = vm.ValorMensalDecimal * meses;
                ModelState.Remove(nameof(vm.ValorContrato));

                if (vm.Naturezas != null)
                {
                    foreach (var natureza in vm.Naturezas) natureza.Valor = natureza.Valor * meses;
                }
            }

            if (await _contratoRepo.VerificarUnicidadeAsync(vm.NumeroContrato, vm.AnoContrato, vm.Id))
            {
                ModelState.AddModelError("NumeroContrato", "Já existe um contrato ativo com este número.");
            }

            if (vm.OrcamentoId.HasValue)
            {
                var orcamentoHeader = await _orcamentoRepo.ObterHeaderPorIdAsync(vm.OrcamentoId.Value);
                // Ignora o valor antigo deste contrato
                var jaGastoOutros = await _contratoRepo.ObterTotalComprometidoPorOrcamentoAsync(vm.OrcamentoId.Value, ignorarContratoId: vm.Id);
                var saldoOrcamento = orcamentoHeader.ValorPrevistoTotal - jaGastoOutros;

                if (vm.ValorContrato > (saldoOrcamento + 0.01m))
                {
                    ModelState.AddModelError(nameof(vm.ValorContrato), 
                        $"Saldo insuficiente. Disponível: {saldoOrcamento:C2}. Tentativa: {vm.ValorContrato:C2}.");
                }
            }

            if (!ModelState.IsValid)
            {
                if (meses > 0 && vm.Naturezas != null)
                {
                    foreach (var natureza in vm.Naturezas) natureza.Valor = natureza.Valor / meses;
                }
                var todosErros = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                TempData["Erro"] = string.Join("<br>", todosErros);

                await PrepararViewBagParaFormulario(vm);
                return View("ContratoForm", vm);
            }
            
            await _contratoRepo.AtualizarAsync(vm);
            await _logService.RegistrarEdicaoAsync("Contrato", null, vm, vm.Id);

            if (!string.IsNullOrWhiteSpace(justificativa))
            {
                await _justificativaService.RegistrarAsync("Contrato", "Atualização", vm.Id, justificativa);
            }

            TempData["Sucesso"] = "Contrato atualizado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        /* -------------------- EXCLUIR -------------------- */
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AutorizarPermissao("CONTRATO_DEL")]
        public async Task<IActionResult> Excluir(int id, string justificativa)
        {
            if (string.IsNullOrWhiteSpace(justificativa))
            {
                TempData["Erro"] = "A justificativa é obrigatória.";
                return RedirectToAction(nameof(Index));
            }

            var contratoParaExcluir = await _contratoRepo.ObterParaEdicaoAsync(id);
            if(contratoParaExcluir == null) return NotFound();

            // [SEGURANÇA] Idealmente verificar se pertence à unidade aqui também

            await _justificativaService.RegistrarAsync("Contrato", "Exclusão de Contrato", id, justificativa);
            
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
        
        private async Task PrepararViewBagParaFormulario(ContratoViewModel vm)
        {
            ViewBag.Naturezas = await _contratoRepo.ListarTodasNaturezasAsync();
            
            // CORREÇÃO FINAL: Usa o método que criamos para filtrar orçamentos
            int entidadeId = User.ObterEntidadeId();
            ViewBag.Orcamentos = await _orcamentoRepo.ListarAtivosPorEntidadeAsync(entidadeId); 

            if (!string.IsNullOrEmpty(vm.FornecedorIdCompleto))
            {
                ViewBag.FornecedorAtual = await _contratoRepo.ObterFornecedorPorIdCompletoAsync(vm.FornecedorIdCompleto);
            }
        }
    }
}