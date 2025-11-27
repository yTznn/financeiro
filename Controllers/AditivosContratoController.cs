using Microsoft.AspNetCore.Mvc;
using Financeiro.Models.ViewModels;
using Financeiro.Repositorios;
using Financeiro.Servicos;
using System.Threading.Tasks;
using System;
using Financeiro.Models;
using Microsoft.AspNetCore.Authorization;

namespace Financeiro.Controllers
{
    [Authorize]

    public class AditivosContratoController : Controller
    {
        private readonly IContratoVersaoRepositorio _versaoRepo;
        private readonly IContratoRepositorio _contratoRepo;
        private readonly IOrcamentoRepositorio _orcamentoRepo; // [NOVO] Necessário para checar saldo
        private readonly IContratoVersaoService _service;
        private readonly ILogService _logService;
        private readonly IJustificativaService _justificativaService;

        public AditivosContratoController(
            IContratoVersaoRepositorio versaoRepo,
            IContratoRepositorio contratoRepo,
            IOrcamentoRepositorio orcamentoRepo, // Injeção Nova
            IContratoVersaoService service,
            ILogService logService,
            IJustificativaService justificativaService)
        {
            _versaoRepo = versaoRepo;
            _contratoRepo = contratoRepo;
            _orcamentoRepo = orcamentoRepo;
            _service = service;
            _logService = logService;
            _justificativaService = justificativaService;
        }

        [HttpGet]
        public async Task<IActionResult> Novo(int contratoId)
        {
            var versaoAtual = await _versaoRepo.ObterVersaoAtualAsync(contratoId);
            
            if (versaoAtual == null)
            {
                var contrato = await _contratoRepo.ObterParaEdicaoAsync(contratoId);
                if(contrato != null)
                {
                    ViewBag.VersaoAtual = new ContratoVersao 
                    { 
                        Versao = 1, 
                        ValorContrato = contrato.ValorContrato,
                        DataInicio = contrato.DataInicio,
                        DataFim = contrato.DataFim,
                        ObjetoContrato = contrato.ObjetoContrato
                    };
                }
            }
            else 
            {
                ViewBag.VersaoAtual = versaoAtual;
            }

            var vm = new AditivoContratoViewModel { ContratoId = contratoId };
            return View("AditivoContratoForm", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Salvar(AditivoContratoViewModel vm)
        {
            // 1. Preenchimento Automático de Datas (Continuidade)
            if (!vm.DataInicioAditivo.HasValue || !vm.NovaDataFim.HasValue)
            {
                var atual = await _versaoRepo.ObterVersaoAtualAsync(vm.ContratoId);
                if (atual != null)
                {
                    if (!vm.DataInicioAditivo.HasValue) vm.DataInicioAditivo = atual.DataInicio;
                    if (!vm.NovaDataFim.HasValue) vm.NovaDataFim = atual.DataFim;
                }
                else
                {
                    var pai = await _contratoRepo.ObterParaEdicaoAsync(vm.ContratoId);
                    if(pai != null)
                    {
                        if (!vm.DataInicioAditivo.HasValue) vm.DataInicioAditivo = pai.DataInicio;
                        if (!vm.NovaDataFim.HasValue) vm.NovaDataFim = pai.DataFim;
                    }
                }
            }

            // ==============================================================================
            // [NOVO] 2. TRAVA DE ORÇAMENTO NO ADITIVO
            // ==============================================================================
            
            // Apenas calculamos se for um aditivo que AUMENTA valor
            if (vm.TipoAditivo == TipoAditivo.Acrescimo || vm.TipoAditivo == TipoAditivo.PrazoAcrescimo)
            {
                var contratoPai = await _contratoRepo.ObterParaEdicaoAsync(vm.ContratoId);
                
                // Só valida se tiver orçamento vinculado
                if (contratoPai != null && contratoPai.OrcamentoId.HasValue)
                {
                    var orcamento = await _orcamentoRepo.ObterHeaderPorIdAsync(contratoPai.OrcamentoId.Value);
                    
                    // Quanto já foi gasto nesse orçamento por OUTROS contratos
                    var gastoOutros = await _contratoRepo.ObterTotalComprometidoPorOrcamentoAsync(
                        contratoPai.OrcamentoId.Value, 
                        ignorarContratoId: vm.ContratoId); // Ignoramos o contrato atual para somar o novo valor total dele
                    
                    var saldoDisponivel = orcamento.ValorPrevistoTotal - gastoOutros;

                    // --- SIMULAÇÃO DO NOVO VALOR TOTAL ---
                    // Precisamos repetir a lógica de cálculo do Service aqui para validar antes de salvar
                    decimal valorAtual = contratoPai.ValorContrato;
                    decimal delta = Math.Abs(vm.NovoValorDecimal); // Usa a propriedade decimal da ViewModel

                    if (vm.EhValorMensal)
                    {
                        // Lógica de meses
                        DateTime iniCalc = vm.DataInicioAditivo ?? DateTime.Today;
                        DateTime fimCalc = vm.NovaDataFim ?? contratoPai.DataFim;
                        int meses = ((fimCalc.Year - iniCalc.Year) * 12) + fimCalc.Month - iniCalc.Month + 1;
                        if (meses < 0) meses = 0;
                        delta = delta * meses;
                    }

                    decimal novoValorTotalContrato = valorAtual + delta;

                    if (novoValorTotalContrato > saldoDisponivel)
                    {
                         TempData["MensagemErro"] = $"Saldo insuficiente no Orçamento. Disponível: {saldoDisponivel:C2}. O contrato iria para: {novoValorTotalContrato:C2}.";
                         ViewBag.VersaoAtual = await _versaoRepo.ObterVersaoAtualAsync(vm.ContratoId);
                         return View("AditivoContratoForm", vm);
                    }
                }
            }
            // ==============================================================================

            if (!ModelState.IsValid)
            {
                ViewBag.VersaoAtual = await _versaoRepo.ObterVersaoAtualAsync(vm.ContratoId);
                return View("AditivoContratoForm", vm);
            }

            try 
            {
                await _service.CriarAditivoAsync(vm);
                await _logService.RegistrarCriacaoAsync("ContratoAditivo", vm, vm.ContratoId);

                TempData["MensagemSucesso"] = "Aditivo do contrato salvo com sucesso!";
                return RedirectToAction("Editar", "Contratos", new { id = vm.ContratoId });
            }
            catch (Exception ex)
            {
                TempData["MensagemErro"] = $"Erro ao salvar aditivo: {ex.Message}";
                ViewBag.VersaoAtual = await _versaoRepo.ObterVersaoAtualAsync(vm.ContratoId);
                return View("AditivoContratoForm", vm);
            }
        }

        /* ========== CANCELAR ÚLTIMO ADITIVO (CORRIGIDO) ========== */
        [HttpPost]
        public async Task<IActionResult> CancelarAditivo([FromBody] CancelarAditivoViewModel body)
        {
            try
            {
                var atual = await _versaoRepo.ObterVersaoAtualAsync(body.ContratoId);
                
                if (atual == null)
                    return BadRequest("Nenhum aditivo encontrado.");

                // [TRAVA DE SEGURANÇA] Não permitir cancelar a Versão 1 (Original)
                if (atual.Versao <= 1)
                {
                    return BadRequest("Não é possível cancelar a versão original do contrato. Use a exclusão de contrato.");
                }

                if (atual.Versao != body.Versao)
                    return BadRequest("A versão informada não é a última vigente.");

                // 1. Exclui APENAS o registro na tabela ContratoVersao
                await _versaoRepo.ExcluirAsync(atual.Id);

                // 2. Busca a versão anterior (que agora se tornou a última)
                var anterior = await _versaoRepo.ObterVersaoAtualAsync(body.ContratoId);
                
                // 3. Atualiza o contrato "Pai" com os dados da versão anterior
                if (anterior != null)
                {
                    // O método Restaurar do repositório faz um UPDATE na tabela Contrato
                    await _versaoRepo.RestaurarContratoAPartirDaVersaoAsync(anterior);
                }
                else
                {
                    // Se por algum motivo bizarro não tiver anterior (e não era v1), temos um problema de integridade.
                    // Mas a trava do "Versao <= 1" deve impedir de chegar aqui.
                    return StatusCode(500, "Erro de integridade: Versão anterior não encontrada.");
                }

                await _logService.RegistrarExclusaoAsync("ContratoAditivo", atual, atual.Id);

                await _justificativaService.RegistrarAsync(
                    "ContratoAditivo",
                    $"Cancelamento da versão {atual.Versao}",
                    atual.Id,
                    body.Justificativa);

                return Ok(new { message = "Aditivo cancelado com sucesso." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erro interno: {ex.Message}");
            }
        }
    }
}