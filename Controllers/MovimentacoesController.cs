using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Financeiro.Models.ViewModels;
using Financeiro.Repositorios;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Globalization; 
using Financeiro.Servicos; 
using Financeiro.Servicos.Anexos; 
using Microsoft.AspNetCore.Authorization;
using Financeiro.Atributos; 
using Financeiro.Extensions; 
using System.Collections.Generic;
using Financeiro.Models;

namespace Financeiro.Controllers
{
    [Authorize]
    public class MovimentacoesController : Controller
    {
        private readonly IMovimentacaoRepositorio _movRepo;
        private readonly IInstrumentoRepositorio _instRepo;
        private readonly IOrcamentoRepositorio _orcamentoRepo;
        private readonly IContratoRepositorio _contratoRepo;
        private readonly IFornecedorRepositorio _fornecedorRepo;
        private readonly IEntidadeRepositorio _entidadeRepo; 
        private readonly ILogService _logService;
        private readonly IJustificativaService _justificativaService;
        private readonly IAnexoService _anexoService;

        public MovimentacoesController(
            IMovimentacaoRepositorio movRepo,
            IInstrumentoRepositorio instRepo,
            IOrcamentoRepositorio orcamentoRepo,
            IContratoRepositorio contratoRepo,
            IFornecedorRepositorio fornecedorRepo,
            IEntidadeRepositorio entidadeRepo,
            ILogService logService,
            IJustificativaService justificativaService,
            IAnexoService anexoService)
        {
            _movRepo = movRepo;
            _instRepo = instRepo;
            _orcamentoRepo = orcamentoRepo;
            _contratoRepo = contratoRepo;
            _fornecedorRepo = fornecedorRepo;
            _entidadeRepo = entidadeRepo;
            _logService = logService;
            _justificativaService = justificativaService;
            _anexoService = anexoService;
        }

        /* ================= LISTAGEM ================= */
        
        [AutorizarPermissao("MOVIMENTACAO_VIEW")]
        public async Task<IActionResult> Index()
        {
            var lista = await _movRepo.ListarAsync();
            
            var listaOrdenada = lista
                .OrderByDescending(x => x.DataMovimentacao)
                .ThenByDescending(x => x.Id);

            return View(listaOrdenada);
        }

        /* ================= CADASTRO E EDIÇÃO ================= */

        [AutorizarPermissao("MOVIMENTACAO_ADD")]
        public async Task<IActionResult> Novo()
        {
            await CarregarCombos();
            
            int entidadeId = User.ObterEntidadeId();
            var entidade = await _entidadeRepo.GetByIdAsync(entidadeId); 
            
            string descricaoContaOrigem = null;
            if (entidade?.ContaBancaria != null)
            {
                descricaoContaOrigem = $"{entidade.ContaBancaria.Banco} | Ag: {entidade.ContaBancaria.Agencia} | Cc: {entidade.ContaBancaria.Conta}";
            }

            var vm = new MovimentacaoViewModel 
            { 
                DataMovimentacao = DateTime.Today,
                ReferenciaMesAno = DateTime.Today.ToString("yyyy-MM"),
                ContaOrigemDescricao = descricaoContaOrigem 
            };
            
            return View("Form", vm);
        }

        [HttpGet]
        [AutorizarPermissao("MOVIMENTACAO_EDIT")]
        public async Task<IActionResult> Editar(int id)
        {
            var vm = await _movRepo.ObterCompletoPorIdAsync(id);
            if (vm == null) return NotFound();

            var anexo = await _anexoService.ObterPorReferenciaAsync("MovimentacaoFinanceira", id);
            vm.TemAnexoSalvo = anexo != null;

            int entidadeId = User.ObterEntidadeId();
            var entidade = await _entidadeRepo.GetByIdAsync(entidadeId);
            
            if (entidade?.ContaBancaria != null)
            {
                vm.ContaOrigemDescricao = $"{entidade.ContaBancaria.Banco} | Ag: {entidade.ContaBancaria.Agencia} | Cc: {entidade.ContaBancaria.Conta}";
            }

            await CarregarCombos();
            return View("Form", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Salvar(MovimentacaoViewModel vm)
        {
            bool isEdicao = vm.Id > 0;

            if (isEdicao && !User.TemPermissao("MOVIMENTACAO_EDIT")) return Forbid();
            if (!isEdicao && !User.TemPermissao("MOVIMENTACAO_ADD")) return Forbid();

            var avisos = new List<string>();

            try 
            {
                // 1. Prepara Dados Iniciais e Validações Básicas
                int entidadeId = User.ObterEntidadeId();
                var entidade = await _entidadeRepo.GetByIdAsync(entidadeId);
                
                if (entidade?.ContaBancariaId == null) ModelState.AddModelError("", "A Entidade logada não possui conta bancária principal vinculada.");
                else vm.ContaOrigemDescricao = $"{entidade.ContaBancaria.Banco} | Ag: {entidade.ContaBancaria.Agencia} | Cc: {entidade.ContaBancaria.Conta}";

                // --- CAPTURA FORNECEDOR (CRUCIAL PARA O DEDO DURO) ---
                int idForn = 0;
                string tipoForn = "";
                
                if (!string.IsNullOrEmpty(vm.FornecedorIdCompleto))
                {
                    var partes = vm.FornecedorIdCompleto.Split('-');
                    if (partes.Length == 2)
                    {
                        tipoForn = partes[0];
                        idForn = int.Parse(partes[1]);
                        
                        // Valida conta bancária
                        var contaForn = await _fornecedorRepo.ObterContaPrincipalAsync(idForn, tipoForn);
                        if (contaForn == null) ModelState.AddModelError("FornecedorIdCompleto", "O fornecedor não tem conta bancária principal.");
                    }
                }

                // --- CAPTURA DATAS ---
                if (!string.IsNullOrEmpty(vm.ReferenciaMesAno))
                {
                    var partesData = vm.ReferenciaMesAno.Split('-');
                    if (partesData.Length == 2 && int.TryParse(partesData[0], out int ano) && int.TryParse(partesData[1], out int mes))
                    {
                        vm.DataReferenciaInicio = new DateTime(ano, mes, 1);
                        vm.DataReferenciaFim = vm.DataReferenciaInicio.Value.AddMonths(1).AddDays(-1);
                    }
                    else ModelState.AddModelError("ReferenciaMesAno", "Data inválida.");
                }

                // 2. Validação dos Rateios
                if (vm.Rateios == null || !vm.Rateios.Any()) 
                {
                    ModelState.AddModelError("", "Adicione itens ao rateio.");
                }
                else
                {
                    var soma = vm.Rateios.Sum(x => x.ValorDecimal);
                    if (Math.Abs(vm.ValorTotalDecimal - soma) > 0.05m) 
                        ModelState.AddModelError("ValorTotal", $"Soma do rateio ({soma:C}) difere do total ({vm.ValorTotalDecimal:C}).");

                    // ==================================================================================
                    // LÓGICA DO DEDO DURO (PÓS-SUBMIT)
                    // ==================================================================================
                    if (vm.DataReferenciaInicio.HasValue)
                    {
                        foreach (var item in vm.Rateios)
                        {
                            // A) Se tem contrato: Valida VIGÊNCIA (Erro)
                            if (item.ContratoId.HasValue && item.ContratoId > 0)
                            {
                                var contrato = await _contratoRepo.ObterParaEdicaoAsync(item.ContratoId.Value);
                                if (contrato != null && (vm.DataReferenciaFim < contrato.DataInicio || vm.DataReferenciaInicio > contrato.DataFim))
                                {
                                    ModelState.AddModelError("", $"Contrato nº {contrato.NumeroContrato} fora de vigência para o período selecionado.");
                                }
                            }
                            // B) Se é Avulso: Verifica se EXISTE CONTRATO (Aviso Amarelo)
                            else if (item.OrcamentoDetalheId > 0 && idForn > 0)
                            {
                                // Chama o repositório ajustado
                                var contratoExistente = await _contratoRepo.ObterContratoAtivoPorItemAsync(idForn, tipoForn, item.OrcamentoDetalheId, vm.DataReferenciaInicio.Value);
                                
                                if (contratoExistente != null)
                                {
                                    string nomeItem = await _orcamentoRepo.ObterNomeItemAsync(item.OrcamentoDetalheId);
                                    avisos.Add($"<b>Atenção:</b> O item '{nomeItem}' foi lançado AVULSO, mas existe o <b>Contrato nº {contratoExistente.NumeroContrato}</b> vigente.");
                                }
                            }

                            // C) Valida Orçamento (Aviso Amarelo)
                            if (item.OrcamentoDetalheId > 0)
                            {
                                int? ignorarId = isEdicao ? vm.Id : null;
                                decimal saldo = await _orcamentoRepo.ObterSaldoDisponivelAsync(item.OrcamentoDetalheId, ignorarId);
                                if (item.ValorDecimal > saldo)
                                {
                                    string nomeItem = await _orcamentoRepo.ObterNomeItemAsync(item.OrcamentoDetalheId);
                                    avisos.Add($"Orçamento Estourado: Item '{nomeItem}' (Saldo: {saldo:C}, Lançado: {item.ValorDecimal:C}).");
                                }
                            }
                        }
                    }
                }

                if (!ModelState.IsValid)
                {
                    await CarregarCombos(); 
                    return View("Form", vm);
                }

                // 3. Persistência
                if (isEdicao)
                {
                    var anterior = await _movRepo.ObterCompletoPorIdAsync(vm.Id);
                    await _movRepo.AtualizarAsync(vm);
                    await _logService.RegistrarEdicaoAsync("MovimentacaoFinanceira", anterior, vm, vm.Id);
                    if (vm.ArquivoAnexo != null) await _anexoService.SalvarAnexoAsync(vm.ArquivoAnexo, "MovimentacaoFinanceira", vm.Id);
                }
                else
                {
                    int novoId = await _movRepo.InserirAsync(vm);
                    if (vm.ArquivoAnexo != null) await _anexoService.SalvarAnexoAsync(vm.ArquivoAnexo, "MovimentacaoFinanceira", novoId);
                    await _logService.RegistrarCriacaoAsync("MovimentacaoFinanceira", vm, novoId);
                }

                // 4. Define Mensagem Final
                if (avisos.Any())
                {
                    TempData["Alerta"] = string.Join("<br><br>", avisos); // Aqui define o Amarelo
                    TempData["Sucesso"] = "Lançamento salvo, mas verifique os alertas!";
                }
                else
                {
                    TempData["Sucesso"] = "Operação realizada com sucesso!";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Erro crítico: {ex.Message}";
                await CarregarCombos();
                return View("Form", vm);
            }
        }

        /* ================= ESTORNO E EXCLUSÃO ================= */

        [HttpPost]
        [AutorizarPermissao("MOVIMENTACAO_ESTORNAR")]
        public async Task<IActionResult> Estornar(int id, string justificativa)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(justificativa)) return BadRequest("Justificativa obrigatória.");

                var original = await _movRepo.ObterCompletoPorIdAsync(id);
                if (original == null) return NotFound();

                var ptBR = new CultureInfo("pt-BR");
                var estorno = new MovimentacaoViewModel
                {
                    DataMovimentacao = DateTime.Today,
                    FornecedorIdCompleto = original.FornecedorIdCompleto,
                    ValorTotal = (original.ValorTotalDecimal * -1).ToString("N2", ptBR),
                    Historico = $"[ESTORNO REF #{original.Id}] {justificativa}",
                    DataReferenciaInicio = original.DataReferenciaInicio, 
                    DataReferenciaFim = original.DataReferenciaFim,
                    Rateios = original.Rateios.Select(r => new MovimentacaoRateioViewModel
                    {
                        InstrumentoId = r.InstrumentoId,
                        ContratoId = r.ContratoId,
                        OrcamentoDetalheId = r.OrcamentoDetalheId,
                        Valor = (r.ValorDecimal * -1).ToString("N2", ptBR)
                    }).ToList()
                };

                int idEstorno = await _movRepo.InserirAsync(estorno);
                await _justificativaService.RegistrarAsync("MovimentacaoFinanceira", "Estorno", id, justificativa);
                await _logService.RegistrarCriacaoAsync("MovimentacaoFinanceira", estorno, idEstorno);

                return Ok(new { message = "Estorno realizado!" });
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        [HttpPost]
        [AutorizarPermissao("MOVIMENTACAO_DEL")]
        public async Task<IActionResult> Excluir(int id, string justificativa)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(justificativa)) return BadRequest("Justificativa obrigatória.");
                var item = await _movRepo.ObterCompletoPorIdAsync(id);
                if (item == null) return NotFound();

                await _justificativaService.RegistrarAsync("MovimentacaoFinanceira", "Exclusão Definitiva", id, justificativa);
                await _logService.RegistrarExclusaoAsync("MovimentacaoFinanceira", item, id);
                await _movRepo.ExcluirAsync(id);

                return Ok(new { message = "Excluído com sucesso." });
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        /* ================= ANEXOS E AJAX ================= */

        [HttpGet]
        public async Task<IActionResult> BaixarAnexo(int id)
        {
            var arquivo = await _anexoService.ObterPorReferenciaAsync("MovimentacaoFinanceira", id);
            if (arquivo == null) return NotFound();
            return File(arquivo.Conteudo, arquivo.ContentType, arquivo.NomeOriginal);
        }

        // --- AJAX: Busca Contratos Ativos do Fornecedor ---
        [HttpGet]
        public async Task<IActionResult> ObterContratosPorFornecedor(string fornecedorIdCompleto)
        {
            if (string.IsNullOrEmpty(fornecedorIdCompleto)) return Json(new List<object>());
            var partes = fornecedorIdCompleto.Split('-');
            if (partes.Length != 2) return Json(new List<object>());
            string tipo = partes[0]; 
            if (!int.TryParse(partes[1], out int idFornecedor)) return Json(new List<object>());

            int entidadeId = User.ObterEntidadeId();
            var contratos = await _contratoRepo.ListarAtivosPorFornecedorAsync(entidadeId, idFornecedor, tipo);

            return Json(contratos.Select(c => new { id = c.Id, numero = c.NumeroContrato, objeto = c.ObjetoContrato }));
        }

        // --- AJAX: Verifica Conta do Fornecedor ---
        [HttpGet]
        public async Task<IActionResult> ObterContaFornecedor(string idCompleto)
        {
            if (string.IsNullOrEmpty(idCompleto)) return Json(new { temConta = false });
            var partes = idCompleto.Split('-');
            if (partes.Length != 2) return Json(new { temConta = false });
            string tipo = partes[0];
            int id = int.Parse(partes[1]);

            var conta = await _fornecedorRepo.ObterContaPrincipalAsync(id, tipo);
            
            if (conta != null)
                return Json(new { temConta = true, descricao = $"{conta.Banco} - Ag: {conta.Agencia} Cc: {conta.Conta}" });
            
            return Json(new { temConta = false });
        }

        // --- AJAX: Itens para Lançamento AVULSO (Analíticos) ---
        [HttpGet]
        public async Task<IActionResult> ObterItensAvulsos(int instrumentoId)
        {
            if (instrumentoId <= 0) return Json(new List<object>());
            var itens = await _orcamentoRepo.ListarItensAnaliticosParaComboAsync(instrumentoId);
            return Json(itens.Select(i => new { id = i.Id, text = i.Nome, saldo = i.Saldo }));
        }

        // --- AJAX: Itens para Lançamento VINCULADO (Contrato + Valor Sugerido) ---
        [HttpGet]
        [AutorizarPermissao("MOVIMENTACAO_ADD")] 
        public async Task<IActionResult> ObterItensPorContrato(int contratoId)
        {
            if (contratoId <= 0) return Json(new List<object>());
            
            // Usa o novo método do repositório que calcula o valor mensal
            var itens = await _contratoRepo.ListarItensDoContratoComValoresAsync(contratoId);
            
            return Json(itens.Select(i => new { id = i.Id, text = i.Nome, valorMensal = i.ValorMensalSugerido }));
        }

        // --- AJAX ATUALIZADO: O "Dedo Duro" agora recebe a DATA ---
        [HttpGet]
        public async Task<IActionResult> VerificarContratoExistente(string fornecedorIdCompleto, int orcamentoDetalheId, string dataReferencia)
        {
            // Validações básicas
            if (string.IsNullOrEmpty(fornecedorIdCompleto) || orcamentoDetalheId <= 0 || string.IsNullOrEmpty(dataReferencia)) 
                return Json(new { existe = false });

            var partes = fornecedorIdCompleto.Split('-');
            if (partes.Length != 2) return Json(new { existe = false });
            string tipo = partes[0]; 
            int idFornecedor = int.Parse(partes[1]);

            // Converte a string "yyyy-MM" (do input type=month) para DateTime (Dia 1)
            var partesData = dataReferencia.Split('-');
            if (partesData.Length < 2) return Json(new { existe = false });
            
            // Cria data base (Dia 1 do mês selecionado)
            var dataRef = new DateTime(int.Parse(partesData[0]), int.Parse(partesData[1]), 1);

            // Passa a data correta para o repositório verificar
            var contrato = await _contratoRepo.ObterContratoAtivoPorItemAsync(idFornecedor, tipo, orcamentoDetalheId, dataRef);

            if (contrato != null)
            {
                return Json(new { 
                    existe = true, 
                    numero = contrato.NumeroContrato,
                    msg = $"Atenção! Existe o Contrato nº {contrato.NumeroContrato} vigente em {dataReferencia} para este fornecedor e este item de despesa."
                });
            }
            return Json(new { existe = false });
        }

        private async Task CarregarCombos()
        {
            int entidadeId = User.ObterEntidadeId(); 
            var instrumentos = await _instRepo.ListarAsync(); 
            var instrumentosFiltrados = instrumentos
                .Where(x => x.EntidadeId == entidadeId && x.Ativo && x.Vigente)
                .OrderByDescending(x => x.DataInicio);

            ViewBag.Instrumentos = new SelectList(instrumentosFiltrados, "Id", "Numero");

            var fornecedores = await _fornecedorRepo.ListarTodosParaComboAsync();
            var listaFornecedores = fornecedores.Select(f => new { Id = f.IdCompleto, Nome = $"{f.Nome} ({f.Documento})" });
            ViewBag.Fornecedores = new SelectList(listaFornecedores, "Id", "Nome");
        }
    }
}