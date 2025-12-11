using Microsoft.AspNetCore.Mvc;
using Financeiro.Repositorios;
using System.Threading.Tasks;
using Financeiro.Models.ViewModels;
using System.Linq;
using Financeiro.Servicos;
using System;
using Microsoft.AspNetCore.Authorization;
using Financeiro.Extensions; // <--- ADICIONE ESTA LINHA

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
            // [CORREÇÃO] 1. Cálculo matemático usando a propriedade auxiliar que converte String -> Decimal
            int meses = ((vm.DataFim.Year - vm.DataInicio.Year) * 12) + vm.DataFim.Month - vm.DataInicio.Month + 1;
            if (meses < 1) meses = 1;

            // Verifica se o valor convertido é válido
            if (vm.ValorMensalDecimal > 0)
            {
                // Multiplica o valor MENSAL (convertido) pelos meses para achar o TOTAL do contrato
                vm.ValorContrato = vm.ValorMensalDecimal * meses;
                
                // Remove erros de validação do Total (pois acabamos de calculá-lo corretamente)
                ModelState.Remove(nameof(vm.ValorContrato));
                
                // Multiplica as naturezas (que vêm mensais da tela) para Totais (para o banco)
                if (vm.Naturezas != null)
                {
                    foreach (var natureza in vm.Naturezas)
                    {
                        natureza.Valor = natureza.Valor * meses;
                    }
                }
            }

            // Validação de unicidade
            if (await _contratoRepo.VerificarUnicidadeAsync(vm.NumeroContrato, vm.AnoContrato))
            {
                ModelState.AddModelError("NumeroContrato", "Já existe um contrato ativo com este número para o ano selecionado.");
            }

            // 2. TRAVA DE ORÇAMENTO
            if (vm.OrcamentoId.HasValue)
            {
                var orcamentoHeader = await _orcamentoRepo.ObterHeaderPorIdAsync(vm.OrcamentoId.Value);
                var jaGasto = await _contratoRepo.ObterTotalComprometidoPorOrcamentoAsync(vm.OrcamentoId.Value);
                var saldoOrcamento = orcamentoHeader.ValorPrevistoTotal - jaGasto;

                // Compara o Total Calculado com o Saldo Disponível
                if (vm.ValorContrato > (saldoOrcamento + 0.01m)) // +0.01 margem de erro
                {
                    ModelState.AddModelError(nameof(vm.ValorContrato), 
                        $"Saldo insuficiente no Orçamento vinculado. Disponível: {saldoOrcamento:C2}. Necessário: {vm.ValorContrato:C2}.");
                }
            }

            // Validação geral
            if (!ModelState.IsValid)
            {
                // Se der erro, precisamos reverter a multiplicação das naturezas para a View não mostrar valores gigantes
                if (meses > 0 && vm.Naturezas != null)
                {
                    foreach (var natureza in vm.Naturezas) natureza.Valor = natureza.Valor / meses;
                }
                // Nota: Não precisamos "dividir" o ValorMensal porque ele é string e não foi alterado.

                var todosErros = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                TempData["Erro"] = string.Join("<br>", todosErros);

                await PrepararViewBagParaFormulario(vm);
                return View("ContratoForm", vm);
            }
            
            // --- PERSISTÊNCIA ---
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Atualizar(ContratoViewModel vm, string justificativa = null)
        {
            // [CORREÇÃO] 1. Cálculo matemático na edição
            int meses = ((vm.DataFim.Year - vm.DataInicio.Year) * 12) + vm.DataFim.Month - vm.DataInicio.Month + 1;
            if (meses < 1) meses = 1;

            if (vm.ValorMensalDecimal > 0)
            {
                vm.ValorContrato = vm.ValorMensalDecimal * meses;
                ModelState.Remove(nameof(vm.ValorContrato));

                if (vm.Naturezas != null)
                {
                    foreach (var natureza in vm.Naturezas)
                    {
                        natureza.Valor = natureza.Valor * meses;
                    }
                }
            }

            if (await _contratoRepo.VerificarUnicidadeAsync(vm.NumeroContrato, vm.AnoContrato, vm.Id))
            {
                ModelState.AddModelError("NumeroContrato", "Já existe um contrato ativo com este número para o ano selecionado.");
            }

            // 2. TRAVA DE ORÇAMENTO NA EDIÇÃO
            if (vm.OrcamentoId.HasValue)
            {
                var orcamentoHeader = await _orcamentoRepo.ObterHeaderPorIdAsync(vm.OrcamentoId.Value);
                
                // Ignora o valor antigo deste contrato para não duplicar a conta
                var jaGastoOutros = await _contratoRepo.ObterTotalComprometidoPorOrcamentoAsync(vm.OrcamentoId.Value, ignorarContratoId: vm.Id);
                var saldoOrcamento = orcamentoHeader.ValorPrevistoTotal - jaGastoOutros;

                if (vm.ValorContrato > (saldoOrcamento + 0.01m))
                {
                    ModelState.AddModelError(nameof(vm.ValorContrato), 
                        $"Saldo insuficiente no Orçamento. Disponível: {saldoOrcamento:C2}. Necessário: {vm.ValorContrato:C2}.");
                }
            }

            if (!ModelState.IsValid)
            {
                // Reverte Naturezas para visualização mensal em caso de erro
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

            // ADICIONE/VERIFIQUE ESTA LINHA:
            // Precisamos da versão atual para saber se podemos cancelar o último aditivo
            var versaoAtual = await _versaoRepo.ObterVersaoAtualAsync(id);
            ViewBag.VersaoAtual = versaoAtual; 

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
        
        private async Task PrepararViewBagParaFormulario(ContratoViewModel vm)
        {
            ViewBag.Naturezas = await _contratoRepo.ListarTodasNaturezasAsync();
            
            // --- CORREÇÃO AQUI ---
            // 1. Obtém o ID da entidade do usuário logado
            int entidadeId = User.ObterEntidadeId();

            // 2. Busca apenas orçamentos ATIVOS e desta ENTIDADE
            ViewBag.Orcamentos = await _orcamentoRepo.ListarAtivosPorEntidadeAsync(entidadeId); 
            // ---------------------

            if (!string.IsNullOrEmpty(vm.FornecedorIdCompleto))
            {
                ViewBag.FornecedorAtual = await _contratoRepo.ObterFornecedorPorIdCompletoAsync(vm.FornecedorIdCompleto);
            }
        }
    }
}