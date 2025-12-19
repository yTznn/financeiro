using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Financeiro.Repositorios;
using System.Threading.Tasks;
using Financeiro.Models.ViewModels;
using System.Linq;
using Financeiro.Servicos;
using System;
using Microsoft.AspNetCore.Authorization;
using Financeiro.Extensions; 
using Financeiro.Atributos;
using System.Collections.Generic;

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

        private const int TAMANHO_PAGINA = 10; 

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

        [HttpGet]
        [AutorizarPermissao("CONTRATO_VIEW")]
        public async Task<IActionResult> Index(int p = 1)
        {
            int entidadeId = User.ObterEntidadeId();
            if (entidadeId == 0) return RedirectToAction("Login", "Conta");
            if (p < 1) p = 1;

            var (itens, totalPaginas) = await _contratoRepo.ListarPaginadoAsync(entidadeId, p, TAMANHO_PAGINA);
            
            foreach (var item in itens) 
            {
                item.QuantidadeAditivos = await _versaoRepo.ContarPorContratoAsync(item.Contrato.Id);
            }
            
            ViewBag.TotalPaginas = totalPaginas;
            ViewBag.PaginaAtual = p;
            return View(itens);
        }

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AutorizarPermissao("CONTRATO_ADD")]
        public async Task<IActionResult> Salvar(ContratoViewModel vm, string justificativa = null)
        {
            // 1. Calcula a vigência em meses
            int meses = ((vm.DataFim.Year - vm.DataInicio.Year) * 12) + vm.DataFim.Month - vm.DataInicio.Month + 1;
            if (meses < 1) meses = 1;

            // 2. CÁLCULO DE VALORES
            if (vm.Itens != null && vm.Itens.Any())
            {
                decimal somaTotalItens = vm.Itens.Sum(x => x.Valor);
                vm.ValorContrato = somaTotalItens;
                
                decimal valorMensalCalculado = somaTotalItens / meses;
                vm.ValorMensal = valorMensalCalculado.ToString("N2");
            }

            int entidadeId = User.ObterEntidadeId();
            if (await _contratoRepo.VerificarUnicidadeAsync(vm.NumeroContrato, vm.AnoContrato, entidadeId))
            {
                ModelState.AddModelError("NumeroContrato", "Já existe um contrato ativo com este número/ano nesta unidade.");
            }

            // --- CORREÇÃO BLINDADA DE VIGÊNCIA ---
            if (vm.OrcamentoId.HasValue)
            {
                var orcamentoPai = await _orcamentoRepo.ObterHeaderPorIdAsync(vm.OrcamentoId.Value);
                
                if (orcamentoPai != null)
                {
                    // Extraímos apenas a DATA (dia/mês/ano), ignorando qualquer horário (00:00 vs 14:00)
                    DateTime inicioContrato = vm.DataInicio.Date;
                    DateTime fimContrato    = vm.DataFim.Date;
                    DateTime inicioOrcamento = orcamentoPai.VigenciaInicio.Date;
                    DateTime fimOrcamento    = orcamentoPai.VigenciaFim.Date;

                    // Valida Início: Contrato não pode começar ANTES do Orçamento
                    if (inicioContrato < inicioOrcamento)
                    {
                        ModelState.AddModelError("DataInicio", 
                            $"A data de início do contrato ({inicioContrato:dd/MM/yyyy}) não pode ser anterior à do Orçamento ({inicioOrcamento:dd/MM/yyyy}).");
                    }

                    // Valida Fim: Contrato não pode terminar DEPOIS do Orçamento
                    if (fimContrato > fimOrcamento)
                    {
                        ModelState.AddModelError("DataFim", 
                            $"A data fim do contrato ({fimContrato:dd/MM/yyyy}) ultrapassa a vigência do Orçamento ({fimOrcamento:dd/MM/yyyy}).");
                    }
                }
                else
                {
                    ModelState.AddModelError("OrcamentoId", "O Orçamento selecionado não foi encontrado ou não está ativo.");
                }
            }
            // ---------------------------------------------

            // Validação de Saldo
            if (vm.Itens != null)
            {
                foreach (var item in vm.Itens)
                {
                    var detalheItem = await _orcamentoRepo.ObterDetalhePorIdAsync(item.Id);
                    if (detalheItem != null)
                    {
                        var jaGastoNoItem = await _contratoRepo.ObterTotalComprometidoPorDetalheAsync(item.Id);
                        var saldoDisponivelItem = detalheItem.ValorPrevisto - jaGastoNoItem;
                        decimal valorTotalDesteItemNoContrato = item.Valor; 

                        // Adicionei uma margem de segurança de 0.05 para evitar erros de arredondamento
                        if (valorTotalDesteItemNoContrato > (saldoDisponivelItem + 0.05m))
                        {
                            ModelState.AddModelError("SomaItens", 
                                $"Saldo insuficiente no item '{detalheItem.Nome}'. Disponível: {saldoDisponivelItem:C2}. Necessário: {valorTotalDesteItemNoContrato:C2}.");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("SomaItens", $"Item ID {item.Id} não encontrado.");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                var erros = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                TempData["Erro"] = "Erros de validação:<br>" + string.Join("<br>", erros);
                await PrepararViewBagParaFormulario(vm);
                return View("ContratoForm", vm);
            }
            
            try 
            {
                await _contratoRepo.InserirAsync(vm);
                await _versaoService.CriarVersaoInicialAsync(vm);
                await _logService.RegistrarCriacaoAsync("Contrato", vm, vm.Id);

                if (!string.IsNullOrWhiteSpace(justificativa))
                {
                    await _justificativaService.RegistrarAsync("Contrato", "Inserção com itens detalhados", vm.Id, justificativa);
                }

                TempData["Sucesso"] = "Contrato salvo com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao salvar: {ex.Message}";
                await PrepararViewBagParaFormulario(vm);
                return View("ContratoForm", vm);
            }
        }
        [HttpGet]
        [AutorizarPermissao("CONTRATO_EDIT")]
        public async Task<IActionResult> Editar(int id)
        {
            var vm = await _contratoRepo.ObterParaEdicaoAsync(id);
            if (vm == null) return NotFound();
            
            await PrepararViewBagParaFormulario(vm);
            
            var historico = await _versaoRepo.ListarPorContratoAsync(id);
            var versaoAtual = historico.FirstOrDefault();
            var versaoOriginal = historico.LastOrDefault() ?? versaoAtual; 

            ViewBag.VersaoAtual = versaoAtual;
            ViewBag.ValorOriginal = versaoOriginal?.ValorContrato ?? vm.ValorContrato;
            
            return View("ContratoForm", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AutorizarPermissao("CONTRATO_EDIT")]
        public async Task<IActionResult> Atualizar(ContratoViewModel vm, string justificativa = null)
        {
            int meses = ((vm.DataFim.Year - vm.DataInicio.Year) * 12) + vm.DataFim.Month - vm.DataInicio.Month + 1;
            if (meses < 1) meses = 1;

            if (vm.Itens != null && vm.Itens.Any())
            {
                decimal somaTotalItens = vm.Itens.Sum(x => x.Valor);
                vm.ValorContrato = somaTotalItens;
                decimal valorMensalCalculado = somaTotalItens / meses;
                vm.ValorMensal = valorMensalCalculado.ToString("N2");
            }

            int entidadeId = User.ObterEntidadeId();
            if (await _contratoRepo.VerificarUnicidadeAsync(vm.NumeroContrato, vm.AnoContrato, entidadeId, vm.Id))
            {
                ModelState.AddModelError("NumeroContrato", "Já existe um contrato ativo com este número/ano nesta unidade.");
            }

            // --- CORREÇÃO BLINDADA DE VIGÊNCIA ---
            if (vm.OrcamentoId.HasValue)
            {
                var orcamentoPai = await _orcamentoRepo.ObterHeaderPorIdAsync(vm.OrcamentoId.Value);
                
                if (orcamentoPai != null)
                {
                    // Extração explícita da Data (sem hora)
                    DateTime inicioContrato = vm.DataInicio.Date;
                    DateTime fimContrato    = vm.DataFim.Date;
                    DateTime inicioOrcamento = orcamentoPai.VigenciaInicio.Date;
                    DateTime fimOrcamento    = orcamentoPai.VigenciaFim.Date;

                    if (inicioContrato < inicioOrcamento)
                    {
                        ModelState.AddModelError("DataInicio", 
                            $"A data de início do contrato ({inicioContrato:dd/MM/yyyy}) não pode ser anterior à do Orçamento ({inicioOrcamento:dd/MM/yyyy}).");
                    }

                    if (fimContrato > fimOrcamento)
                    {
                        ModelState.AddModelError("DataFim", 
                            $"A data fim do contrato ({fimContrato:dd/MM/yyyy}) ultrapassa a vigência do Orçamento ({fimOrcamento:dd/MM/yyyy}).");
                    }
                }
                else
                {
                    ModelState.AddModelError("OrcamentoId", "O Orçamento selecionado não foi encontrado.");
                }
            }
            // ---------------------------------------------

            if (vm.Itens != null)
            {
                foreach (var item in vm.Itens)
                {
                    var detalheItem = await _orcamentoRepo.ObterDetalhePorIdAsync(item.Id);
                    if (detalheItem != null)
                    {
                        var jaGastoNoItem = await _contratoRepo.ObterTotalComprometidoPorDetalheAsync(item.Id, ignorarContratoId: vm.Id);
                        var saldoDisponivelItem = detalheItem.ValorPrevisto - jaGastoNoItem;
                        decimal valorTotalDesteItemNoContrato = item.Valor;

                        if (valorTotalDesteItemNoContrato > (saldoDisponivelItem + 0.05m))
                        {
                                ModelState.AddModelError("SomaItens", 
                                $"Saldo insuficiente no item '{detalheItem.Nome}'. Disponível: {saldoDisponivelItem:C2}. Necessário: {valorTotalDesteItemNoContrato:C2}.");
                        }
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                var erros = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                TempData["Erro"] = "Erros de validação:<br>" + string.Join("<br>", erros);
                await PrepararViewBagParaFormulario(vm);
                return View("ContratoForm", vm);
            }
            
            try
            {
                await _contratoRepo.AtualizarAsync(vm);
                await _versaoService.AtualizarSnapshotUltimaVersaoAsync(vm.Id);
                await _logService.RegistrarEdicaoAsync("Contrato", null, vm, vm.Id);

                if (!string.IsNullOrWhiteSpace(justificativa))
                    await _justificativaService.RegistrarAsync("Contrato", "Atualização Cadastral/Itens", vm.Id, justificativa);

                TempData["Sucesso"] = "Dados do contrato atualizados com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro ao atualizar: {ex.Message}";
                await PrepararViewBagParaFormulario(vm);
                return View("ContratoForm", vm);
            }
        }

        [HttpPost]
        [AutorizarPermissao("ADITIVO_DEL")]
        public async Task<IActionResult> CancelarUltimoAditivo(int contratoId, int versao, string justificativa)
        {
            try
            {
                var (removida, vigente) = await _versaoService.CancelarUltimoAditivoAsync(contratoId, versao, justificativa);
                
                if (!string.IsNullOrWhiteSpace(justificativa))
                    await _justificativaService.RegistrarAsync("Contrato", $"Cancelamento de Aditivo V.{removida.Versao}", contratoId, justificativa);

                return Json(new { sucesso = true, mensagem = "Último aditivo cancelado com sucesso. O contrato voltou à versão anterior." });
            }
            catch (Exception ex)
            {
                return Json(new { sucesso = false, mensagem = "Erro ao cancelar aditivo: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AutorizarPermissao("CONTRATO_DEL")]
        public async Task<IActionResult> Excluir(int id, string justificativa)
        {
            try
            {
                // 1. Validação Básica: Justificativa
                if (string.IsNullOrWhiteSpace(justificativa)) 
                { 
                    TempData["Erro"] = "Justificativa obrigatória."; 
                    return RedirectToAction(nameof(Index)); 
                }

                // 2. Busca o contrato (para log e para pegar o número no aviso de erro)
                var c = await _contratoRepo.ObterParaEdicaoAsync(id);
                if(c == null) return NotFound();

                // ==============================================================================
                // 3. O GUARDA DE TRÂNSITO (A CORREÇÃO) 👮‍♂️🛑
                // ==============================================================================
                bool temLancamentos = await _contratoRepo.PossuiLancamentosFinanceirosAsync(id);
                
                if (temLancamentos)
                {
                    // Bloqueia e avisa com o número do contrato para facilitar
                    TempData["Erro"] = $"Não é possível excluir o Contrato nº {c.NumeroContrato}: Existem lançamentos financeiros vinculados a ele.";
                    return RedirectToAction(nameof(Index));
                }
                // ==============================================================================

                // 4. Se passou pelo guarda, executa a exclusão
                await _justificativaService.RegistrarAsync("Contrato", "Exclusão", id, justificativa);
                await _contratoRepo.ExcluirAsync(id); // Ou InativarAsync, dependendo do seu padrão
                await _logService.RegistrarExclusaoAsync("Contrato", c, id);
                
                TempData["Sucesso"] = "Excluído com sucesso!";
            }
            catch (Exception ex)
            {
                // Captura erros de banco (como FKs não tratadas)
                TempData["Erro"] = $"Erro crítico ao excluir: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // AJAX e Helpers
        [HttpGet]
        public async Task<IActionResult> SugerirNumero(int ano)
        {
            int entidadeId = User.ObterEntidadeId();
            var n = await _contratoRepo.SugerirProximoNumeroAsync(ano, entidadeId);
            return Json(new { proximoNumero = n });
        }
        
        [HttpGet]
        public async Task<IActionResult> BuscarFornecedores(string term = "", int page = 1)
        {
            var (itens, total) = await _contratoRepo.BuscarFornecedoresPaginadoAsync(term, page, 10);
            return Json(new { results = itens.Select(f => new { id = $"{f.Tipo}-{f.FornecedorId}", text = $"{f.Nome} ({f.Documento})" }), pagination = new { more = (page * 10) < total } });
        }

        [HttpGet]
        public async Task<IActionResult> ListarItensOrcamento(int orcamentoId)
        {
            // Busca apenas os itens que aceitam lançamentos (folhas da árvore)
            var itens = await _orcamentoRepo.ListarDetalhesParaLancamentoAsync(orcamentoId);
            return Json(itens.Select(x => new { id = x.Id, nome = x.Nome }));
        }
        
        [HttpGet]
        public async Task<IActionResult> Historico(int id, int pag = 1)
        {
            var (itens, total) = await _versaoRepo.ListarPaginadoAsync(id, pag);
            ViewBag.TotalPaginas = total;
            ViewBag.PaginaAtual = pag;
            ViewBag.ContratoId = id;
            return PartialView("_HistoricoContrato", itens);
        }
        [HttpGet]
        [AutorizarPermissao("CONTRATO_VIEW")]
        public async Task<IActionResult> VisualizarHistorico(int contratoId, int versao)
        {
            // 1. Busca os dados BASE do contrato (Cabeçalho fixo: Fornecedor, Número, Ano, etc)
            // Usamos o método existente para pegar esses dados mestres já formatados
            var contratoAtual = await _contratoRepo.ObterParaEdicaoAsync(contratoId);
            if (contratoAtual == null) return NotFound("Contrato não encontrado.");

            // 2. Busca o SNAPSHOT DAQUELA ÉPOCA (Tabela ContratoVersao)
            // Aqui pegamos: Valor antigo, Vigência antiga, Objeto antigo
            var dadosHistoricos = await _versaoRepo.ObterPorIdAsync(contratoId, versao);
            if (dadosHistoricos == null) return NotFound("Versão histórica não encontrada.");

            // 3. Busca os ITENS DAQUELA ÉPOCA (Tabela ContratoVersaoItem)
            // Aqui pegamos a lista de itens exata daquele momento
            var itensHistoricos = await _versaoRepo.ListarItensPorVersaoAsync(dadosHistoricos.Id);

            // 4. Monta a ViewModel MESCLANDO (Dados Fixos do Pai + Dados Variáveis do Histórico)
            var vm = new ContratoViewModel
            {
                // --- DADOS FIXOS (Não mudam com aditivo) ---
                Id = contratoAtual.Id, 
                FornecedorIdCompleto = contratoAtual.FornecedorIdCompleto,
                NumeroContrato = contratoAtual.NumeroContrato,
                AnoContrato = contratoAtual.AnoContrato,
                OrcamentoId = contratoAtual.OrcamentoId, // O orçamento pai continua o mesmo
                DataAssinatura = contratoAtual.DataAssinatura,

                // --- DADOS HISTÓRICOS (Vêm do Snapshot) ---
                ObjetoContrato = dadosHistoricos.ObjetoContrato,
                DataInicio = dadosHistoricos.DataInicio,
                DataFim = dadosHistoricos.DataFim,
                ValorContrato = dadosHistoricos.ValorContrato,
                Observacao = dadosHistoricos.Observacao, // Observação registrada na versão
                Ativo = dadosHistoricos.Ativo,

                // --- ITENS HISTÓRICOS (Conversão para ViewModel) ---
                Itens = itensHistoricos.Select(x => new ContratoItemViewModel
                {
                    Id = x.OrcamentoDetalheId, // Mapeia para o ID do detalhe (como na edição normal)
                    NomeItem = x.NomeItem,     // O Repositório já traz o nome via JOIN
                    Valor = x.Valor            // Valor TOTAL histórico
                }).ToList()
            };

            // 5. Recalcula o "Valor Mensal Visual" baseado na vigência HISTÓRICA
            int mesesHistoricos = ((vm.DataFim.Year - vm.DataInicio.Year) * 12) + vm.DataFim.Month - vm.DataInicio.Month + 1;
            if (mesesHistoricos < 1) mesesHistoricos = 1;
            
            if (vm.ValorContrato > 0)
            {
                vm.ValorMensal = (vm.ValorContrato / mesesHistoricos).ToString("N2", new System.Globalization.CultureInfo("pt-BR"));
            }

            // 6. Prepara a View para MODO LEITURA
            // Carregamos as ViewBags normais para os dropdowns não quebrarem (mesmo estando disabled)
            await PrepararViewBagParaFormulario(vm);
            
            ViewBag.Title = $"Histórico - Versão {versao} (Consulta)";
            ViewBag.ApenasLeitura = true; // <--- Essa é a flag que vamos usar na View no próximo passo
            ViewBag.VersaoVisualizada = versao;

            // Reutilizamos a MESMA View de formulário, mas ela vai se comportar diferente por causa da flag
            return View("ContratoForm", vm);
        }

        private async Task PrepararViewBagParaFormulario(ContratoViewModel vm)
        {
            int entidadeId = User.ObterEntidadeId();
            ViewBag.Orcamentos = await _orcamentoRepo.ListarAtivosPorEntidadeAsync(entidadeId); 
            
            if (!string.IsNullOrEmpty(vm.FornecedorIdCompleto)) 
                ViewBag.FornecedorAtual = await _contratoRepo.ObterFornecedorPorIdCompletoAsync(vm.FornecedorIdCompleto);
                
            // Se já tiver um Orçamento Pai selecionado, carrega os filhos possíveis para o dropdown da grid
            if (vm.OrcamentoId.HasValue)
            {
                 var listaItens = await _orcamentoRepo.ListarDetalhesParaLancamentoAsync(vm.OrcamentoId.Value);
                 // Serializa para usar no JS da grid
                 ViewBag.ListaItensOrcamentoJson = System.Text.Json.JsonSerializer.Serialize(listaItens.Select(x => new { x.Id, x.Nome }));
            }
        }
    }
}