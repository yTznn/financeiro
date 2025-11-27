using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Financeiro.Models.ViewModels;
using Financeiro.Repositorios;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Globalization; 
using Financeiro.Servicos;        // LogService, JustificativaService
using Financeiro.Servicos.Anexos; // AnexoService

namespace Financeiro.Controllers
{
    public class MovimentacoesController : Controller
    {
        private readonly IMovimentacaoRepositorio _movRepo;
        private readonly IInstrumentoRepositorio _instRepo;
        private readonly INaturezaRepositorio _natRepo;
        private readonly IContratoRepositorio _contratoRepo;
        
        // Serviços Auxiliares Essenciais
        private readonly ILogService _logService;
        private readonly IJustificativaService _justificativaService;
        private readonly IAnexoService _anexoService;

        public MovimentacoesController(
            IMovimentacaoRepositorio movRepo,
            IInstrumentoRepositorio instRepo,
            INaturezaRepositorio natRepo,
            IContratoRepositorio contratoRepo,
            ILogService logService,
            IJustificativaService justificativaService,
            IAnexoService anexoService)
        {
            _movRepo = movRepo;
            _instRepo = instRepo;
            _natRepo = natRepo;
            _contratoRepo = contratoRepo;
            _logService = logService;
            _justificativaService = justificativaService;
            _anexoService = anexoService;
        }

        public async Task<IActionResult> Index()
        {
            var lista = await _movRepo.ListarAsync();
            return View(lista);
        }

        public async Task<IActionResult> Novo()
        {
            await CarregarCombos();
            return View(new MovimentacaoViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Salvar(MovimentacaoViewModel vm)
        {
            try 
            {
                // 1. Validação de Totais (Soma dos Rateios vs Total Pago)
                if (vm.Rateios != null && vm.Rateios.Any())
                {
                    // Usa .ValorDecimal para somar os números reais (pois a propriedade .Valor é string)
                    var soma = vm.Rateios.Sum(x => x.ValorDecimal);
                    
                    // Tolerância de centavos
                    if (Math.Abs(vm.ValorTotalDecimal - soma) > 0.05m)
                    {
                        ModelState.AddModelError("ValorTotal", $"A soma do rateio ({soma:C}) não bate com o total ({vm.ValorTotalDecimal:C}).");
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Adicione pelo menos um item no rateio (origem do recurso).");
                }

                if (!ModelState.IsValid)
                {
                    // Prepara mensagem de erro para o SweetAlert (via TempData)
                    var erros = string.Join("<br>", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    TempData["Erro"] = erros;
                    
                    await CarregarCombos();
                    return View("Novo", vm);
                }

                // 2. Persistência (O Repositório DEVE retornar o ID gerado)
                int novoId = await _movRepo.InserirAsync(vm);

                // 3. Salvar Anexo (Se o usuário enviou PDF)
                if (vm.ArquivoAnexo != null)
                {
                    // "MovimentacaoFinanceira" é a tabela de origem para o serviço de arquivos
                    await _anexoService.SalvarAnexoAsync(vm.ArquivoAnexo, "MovimentacaoFinanceira", novoId);
                }

                // 4. Auditoria (Log)
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

        // Ação de Estorno (Chamada via AJAX/Fetch da View Index)
        [HttpPost]
        public async Task<IActionResult> Estornar(int id, string justificativa)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(justificativa))
                    return BadRequest("A justificativa é obrigatória para realizar o estorno.");

                // 1. Busca o lançamento original completo (com rateios)
                var original = await _movRepo.ObterCompletoPorIdAsync(id);
                if (original == null) return NotFound("Lançamento original não encontrado.");

                var ptBR = new CultureInfo("pt-BR");

                // 2. Cria o objeto de estorno (Valores Invertidos)
                var estorno = new MovimentacaoViewModel
                {
                    DataMovimentacao = DateTime.Today,
                    FornecedorIdCompleto = original.FornecedorIdCompleto,
                    
                    // Inverte o Valor Total (* -1) e converte para string PT-BR para salvar
                    ValorTotal = (original.ValorTotalDecimal * -1).ToString("N2", ptBR),
                    
                    Historico = $"[ESTORNO REF #{original.Id}] {justificativa}",
                    
                    // Inverte cada item do rateio
                    Rateios = original.Rateios.Select(r => new MovimentacaoRateioViewModel
                    {
                        InstrumentoId = r.InstrumentoId,
                        ContratoId = r.ContratoId,
                        NaturezaId = r.NaturezaId,
                        // Inverte valor do rateio
                        Valor = (r.ValorDecimal * -1).ToString("N2", ptBR)
                    }).ToList()
                };

                // 3. Salva o estorno no banco (Gera novo ID)
                int idEstorno = await _movRepo.InserirAsync(estorno);

                // 4. Registra Auditoria
                await _justificativaService.RegistrarAsync("MovimentacaoFinanceira", "Estorno", id, justificativa);
                await _logService.RegistrarCriacaoAsync("MovimentacaoFinanceira", estorno, idEstorno);

                return Ok(new { message = "Estorno realizado com sucesso! O saldo foi recomposto." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erro técnico ao estornar: {ex.Message}");
            }
        }

        private async Task CarregarCombos()
        {
            var instrumentos = await _instRepo.ListarAsync();
            // Exibe apenas instrumentos ativos
            ViewBag.Instrumentos = new SelectList(instrumentos.Where(x => x.Ativo), "Id", "Numero");

            var naturezas = await _natRepo.ListarTodasAsync();
            ViewBag.Naturezas = new SelectList(naturezas, "Id", "Nome");
            
            // Carrega contratos para o combo (Paginação manual para trazer um bom número inicial)
            // Se tiver muitos contratos, considere usar Select2 com AJAX na view futuramente.
            var contratosData = await _contratoRepo.ListarPaginadoAsync(1, 1000);
            ViewBag.Contratos = new SelectList(contratosData.Itens, "Contrato.Id", "Contrato.NumeroContrato");
        }
        
    }
}