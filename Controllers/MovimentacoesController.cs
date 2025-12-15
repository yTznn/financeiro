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

namespace Financeiro.Controllers
{
    [Authorize]
    public class MovimentacoesController : Controller
    {
        private readonly IMovimentacaoRepositorio _movRepo;
        private readonly IInstrumentoRepositorio _instRepo;
        private readonly INaturezaRepositorio _natRepo;
        private readonly IContratoRepositorio _contratoRepo;
        private readonly IFornecedorRepositorio _fornecedorRepo;
        private readonly ILogService _logService;
        private readonly IJustificativaService _justificativaService;
        private readonly IAnexoService _anexoService;

        public MovimentacoesController(
            IMovimentacaoRepositorio movRepo,
            IInstrumentoRepositorio instRepo,
            INaturezaRepositorio natRepo,
            IContratoRepositorio contratoRepo,
            IFornecedorRepositorio fornecedorRepo,
            ILogService logService,
            IJustificativaService justificativaService,
            IAnexoService anexoService)
        {
            _movRepo = movRepo;
            _instRepo = instRepo;
            _natRepo = natRepo;
            _contratoRepo = contratoRepo;
            _fornecedorRepo = fornecedorRepo;
            _logService = logService;
            _justificativaService = justificativaService;
            _anexoService = anexoService;
        }

        /* ================= LISTAGEM (INDEX) ================= */
        
        [AutorizarPermissao("MOVIMENTACAO_VIEW")]
        public async Task<IActionResult> Index()
        {
            var lista = await _movRepo.ListarAsync();
            var listaOrdenada = lista
                .OrderByDescending(x => x.DataMovimentacao)
                .ThenByDescending(x => x.Id);

            return View(listaOrdenada);
        }

        /* ================= NOVO LANÇAMENTO ================= */

        [AutorizarPermissao("MOVIMENTACAO_ADD")]
        public async Task<IActionResult> Novo()
        {
            await CarregarCombos();
            
            int entidadeId = User.ObterEntidadeId();
            var instrumentos = await _instRepo.ListarAsync();
            var vigente = instrumentos.FirstOrDefault(x => x.EntidadeId == entidadeId && x.Vigente);

            var vm = new MovimentacaoViewModel 
            { 
                DataMovimentacao = DateTime.Today,
                // Sugere o mês atual como referência padrão
                ReferenciaMesAno = DateTime.Today.ToString("yyyy-MM")
            };
            
            ViewBag.InstrumentoVigenteId = vigente?.Id;

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AutorizarPermissao("MOVIMENTACAO_ADD")]
        public async Task<IActionResult> Salvar(MovimentacaoViewModel vm)
        {
            try 
            {
                // 1. Validar Totais do Rateio
                if (vm.Rateios != null && vm.Rateios.Any())
                {
                    var soma = vm.Rateios.Sum(x => x.ValorDecimal);
                    if (Math.Abs(vm.ValorTotalDecimal - soma) > 0.05m)
                    {
                        ModelState.AddModelError("ValorTotal", $"A soma do rateio ({soma:C}) não bate com o total ({vm.ValorTotalDecimal:C}).");
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Adicione pelo menos um item no rateio.");
                }

                // 2. [NOVO] Processar Datas de Referência e Validar Vigência
                if (!string.IsNullOrEmpty(vm.ReferenciaMesAno))
                {
                    // Converte string "yyyy-MM" para Datas
                    var partes = vm.ReferenciaMesAno.Split('-'); // [0]=Ano, [1]=Mês
                    if (partes.Length == 2 && int.TryParse(partes[0], out int ano) && int.TryParse(partes[1], out int mes))
                    {
                        vm.DataReferenciaInicio = new DateTime(ano, mes, 1);
                        vm.DataReferenciaFim = vm.DataReferenciaInicio.Value.AddMonths(1).AddDays(-1);

                        // --- VALIDAÇÃO DE VIGÊNCIA DE CONTRATO ---
                        if (vm.Rateios != null)
                        {
                            foreach (var item in vm.Rateios)
                            {
                                if (item.ContratoId.HasValue && item.ContratoId.Value > 0)
                                {
                                    // Busca dados do contrato para checar vigência
                                    // Precisamos de um método que retorne o contrato completo ou suas datas
                                    var contrato = await _contratoRepo.ObterParaEdicaoAsync(item.ContratoId.Value);
                                    
                                    if (contrato != null)
                                    {
                                        // Regra: O mês de referência deve estar DENTRO da vigência do contrato
                                        // DataInicioRef >= ContratoInicio AND DataFimRef <= ContratoFim
                                        if (vm.DataReferenciaInicio < contrato.DataInicio || vm.DataReferenciaFim > contrato.DataFim)
                                        {
                                            ModelState.AddModelError("", 
                                                $"O contrato {contrato.NumeroContrato} tem vigência de {contrato.DataInicio:MM/yyyy} a {contrato.DataFim:MM/yyyy}. " +
                                                $"Não é possível lançar referência de {mes}/{ano} para ele.");
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("ReferenciaMesAno", "Data de referência inválida.");
                    }
                }

                if (!ModelState.IsValid)
                {
                    var erros = string.Join("<br>", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    TempData["Erro"] = erros;
                    
                    await CarregarCombos(); 
                    return View("Novo", vm);
                }

                // 3. Persistência (Passando a VM com as datas já preenchidas)
                int novoId = await _movRepo.InserirAsync(vm);

                // 4. Salvar Anexo
                if (vm.ArquivoAnexo != null)
                {
                    await _anexoService.SalvarAnexoAsync(vm.ArquivoAnexo, "MovimentacaoFinanceira", novoId);
                }

                // 5. Log
                await _logService.RegistrarCriacaoAsync("MovimentacaoFinanceira", vm, novoId);

                TempData["Sucesso"] = "Pagamento registrado com sucesso!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Erro"] = $"Não foi possível salvar: {ex.Message}";
                await CarregarCombos();
                return View("Novo", vm);
            }
        }

        /* ================= ESTORNO ================= */

        [HttpPost]
        [AutorizarPermissao("MOVIMENTACAO_ESTORNAR")]
        public async Task<IActionResult> Estornar(int id, string justificativa)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(justificativa))
                    return BadRequest("A justificativa é obrigatória.");

                var original = await _movRepo.ObterCompletoPorIdAsync(id);
                if (original == null) return NotFound("Lançamento não encontrado.");

                var ptBR = new CultureInfo("pt-BR");

                // Invert values for reversal
                var estorno = new MovimentacaoViewModel
                {
                    DataMovimentacao = DateTime.Today,
                    FornecedorIdCompleto = original.FornecedorIdCompleto,
                    ValorTotal = (original.ValorTotalDecimal * -1).ToString("N2", ptBR),
                    Historico = $"[ESTORNO REF #{original.Id}] {justificativa}",
                    // Mantém a referência do original para o estorno também? Geralmente sim.
                    DataReferenciaInicio = original.DataReferenciaInicio, 
                    DataReferenciaFim = original.DataReferenciaFim,
                    
                    Rateios = original.Rateios.Select(r => new MovimentacaoRateioViewModel
                    {
                        InstrumentoId = r.InstrumentoId,
                        ContratoId = r.ContratoId,
                        NaturezaId = r.NaturezaId,
                        Valor = (r.ValorDecimal * -1).ToString("N2", ptBR)
                    }).ToList()
                };

                int idEstorno = await _movRepo.InserirAsync(estorno);

                await _justificativaService.RegistrarAsync("MovimentacaoFinanceira", "Estorno", id, justificativa);
                await _logService.RegistrarCriacaoAsync("MovimentacaoFinanceira", estorno, idEstorno);

                return Ok(new { message = "Estorno realizado com sucesso!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erro ao estornar: {ex.Message}");
            }
        }

        /* ================= EXCLUSÃO REAL ================= */

        [HttpPost]
        [AutorizarPermissao("MOVIMENTACAO_DEL")]
        public async Task<IActionResult> Excluir(int id, string justificativa)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(justificativa))
                    return BadRequest("A justificativa é obrigatória para exclusão.");

                var item = await _movRepo.ObterCompletoPorIdAsync(id);
                if (item == null) return NotFound("Lançamento não encontrado.");

                await _justificativaService.RegistrarAsync("MovimentacaoFinanceira", "Exclusão Definitiva", id, justificativa);
                await _logService.RegistrarExclusaoAsync("MovimentacaoFinanceira", item, id);
                await _movRepo.ExcluirAsync(id);

                return Ok(new { message = "Lançamento excluído definitivamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erro ao excluir: {ex.Message}");
            }
        }

        /* ================= ANEXOS E AJAX ================= */

        [HttpGet]
        [AutorizarPermissao("MOVIMENTACAO_VIEW")]
        public async Task<IActionResult> BaixarAnexo(int id)
        {
            var arquivo = await _anexoService.ObterPorReferenciaAsync("MovimentacaoFinanceira", id);
            if (arquivo == null) return NotFound("Nenhum comprovante anexado.");
            return File(arquivo.Conteudo, arquivo.ContentType, arquivo.NomeOriginal);
        }

        [HttpGet]
        [AutorizarPermissao("MOVIMENTACAO_ADD")]
        public async Task<IActionResult> ObterContratosPorFornecedor(string fornecedorIdCompleto)
        {
            if (string.IsNullOrEmpty(fornecedorIdCompleto)) return Json(new List<object>());

            var partes = fornecedorIdCompleto.Split('-');
            if (partes.Length != 2) return Json(new List<object>());

            string tipo = partes[0]; 
            if (!int.TryParse(partes[1], out int idFornecedor)) return Json(new List<object>());

            int entidadeId = User.ObterEntidadeId();
            var contratos = await _contratoRepo.ListarAtivosPorFornecedorAsync(entidadeId, idFornecedor, tipo);

            var resultado = contratos.Select(c => new { 
                id = c.Id, 
                numero = c.NumeroContrato,
                objeto = c.ObjetoContrato 
            });

            return Json(resultado);
        }

        [HttpGet]
        [AutorizarPermissao("MOVIMENTACAO_ADD")] 
        public async Task<IActionResult> ObterNaturezasPorContrato(int contratoId)
        {
            if (contratoId <= 0) return Json(new List<object>());
            var naturezas = await _contratoRepo.ListarNaturezasDetalhadasPorContratoAsync(contratoId);
            var resultado = naturezas.Select(n => new { id = n.Id, text = n.Nome });
            return Json(resultado);
        }

        private async Task CarregarCombos()
        {
            int entidadeId = User.ObterEntidadeId(); 
            var instrumentos = await _instRepo.ListarAsync(); 
            var instrumentosFiltrados = instrumentos
                .Where(x => x.EntidadeId == entidadeId && x.Ativo && x.Vigente)
                .OrderByDescending(x => x.DataInicio);

            ViewBag.Instrumentos = new SelectList(instrumentosFiltrados, "Id", "Numero");

            var naturezas = await _natRepo.ListarTodasAsync();
            ViewBag.Naturezas = new SelectList(naturezas.Where(n => n.Ativo), "Id", "Nome");
            
            var contratosData = await _contratoRepo.ListarPaginadoAsync(1, 2000);
            ViewBag.Contratos = new SelectList(contratosData.Itens, "Contrato.Id", "Contrato.NumeroContrato");

            var fornecedores = await _fornecedorRepo.ListarTodosParaComboAsync();
            var listaFornecedores = fornecedores.Select(f => new 
            { 
                Id = f.IdCompleto, 
                Nome = $"{f.Nome} ({f.Documento})" 
            });

            ViewBag.Fornecedores = new SelectList(listaFornecedores, "Id", "Nome");
        }
    }
}