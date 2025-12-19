using Financeiro.Models;
using Financeiro.Models.ViewModels;
using Financeiro.Repositorios;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions; // Necessário para garantir a integridade (Snapshot + Update)

namespace Financeiro.Servicos
{
    public class ContratoVersaoService : IContratoVersaoService
    {
        private readonly IContratoRepositorio _contratoRepo;
        private readonly IContratoVersaoRepositorio _versaoRepo;

        public ContratoVersaoService(IContratoRepositorio contratoRepo, IContratoVersaoRepositorio versaoRepo)
        {
            _contratoRepo = contratoRepo;
            _versaoRepo = versaoRepo;
        }

        // --- GERAÇÃO DA V1 (SNAPSHOT INICIAL) ---
        public async Task CriarVersaoInicialAsync(ContratoViewModel vm)
        {
            if (vm == null || vm.Id == 0) return;

            // 1. Cria Header
            var versaoInicial = new ContratoVersao
            {
                ContratoId = vm.Id,
                Versao = 1,
                ObjetoContrato = vm.ObjetoContrato,
                DataInicio = vm.DataInicio,
                DataFim = vm.DataFim,
                ValorContrato = vm.ValorContrato,
                TipoAditivo = null,
                Observacao = "Versão original do contrato.",
                DataRegistro = DateTime.Now,
                DataInicioAditivo = null,
                Ativo = true
            };

            int versaoId = await _versaoRepo.InserirAsync(versaoInicial);

            // 2. Snapshot dos ITENS
            if (vm.Itens != null && vm.Itens.Any())
            {
                var itensHistorico = vm.Itens.Select(item => new ContratoVersaoItem
                {
                    ContratoVersaoId = versaoId,
                    OrcamentoDetalheId = item.Id,
                    Valor = item.Valor
                }).ToList();

                await _versaoRepo.InserirItensAsync(itensHistorico);
            }
        }

        // --- GERAÇÃO DE ADITIVO (SNAPSHOT DO ESTADO ATUAL -> ATUALIZAÇÃO PARA O NOVO) ---
        public async Task CriarAditivoAsync(AditivoContratoViewModel vm)
        {
            if (vm == null) throw new ArgumentNullException(nameof(vm));

            // Usamos TransactionScope para que o salvamento do histórico e a atualização do contrato sejam atômicos
            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                // 1. RECUPERAR O ESTADO ATUAL (ANTES DA MUDANÇA)
                // Precisamos dos dados "crus" do banco para salvar no histórico antes de alterar
                var contratoAtual = await _contratoRepo.ObterParaEdicaoAsync(vm.ContratoId);
                if (contratoAtual == null) throw new Exception("Contrato original não encontrado.");

                // Descobrir qual é o número da próxima versão
                var ultimaVersao = await _versaoRepo.ObterVersaoAtualAsync(vm.ContratoId);
                int numeroNovaVersao = (ultimaVersao?.Versao ?? 1) + 1;

                // 2. CRIAR O SNAPSHOT (HISTÓRICO)
                // Salvamos como o contrato ERA até agora (Estado Vigente vira Histórico)
                var versaoHistorica = new ContratoVersao
                {
                    ContratoId = vm.ContratoId,
                    Versao = numeroNovaVersao, // A versão salva no histórico ganha o ID novo
                    
                    // Dados do momento anterior à mudança:
                    ObjetoContrato = contratoAtual.ObjetoContrato,
                    DataInicio = contratoAtual.DataInicio,
                    DataFim = contratoAtual.DataFim,
                    ValorContrato = contratoAtual.ValorContrato,
                    Ativo = contratoAtual.Ativo,
                    
                    // Metadados do Aditivo que está sendo aplicado agora:
                    TipoAditivo = vm.TipoAditivo,
                    Observacao = vm.Justificativa,
                    DataRegistro = DateTime.Now,
                    DataInicioAditivo = vm.DataInicioAditivo
                };

                int idVersao = await _versaoRepo.InserirAsync(versaoHistorica);

                // 3. SALVAR OS ITENS NO HISTÓRICO
                // Copiamos os itens que estavam vigentes para a tabela de versão
                if (contratoAtual.Itens != null && contratoAtual.Itens.Any())
                {
                    var itensHistoricos = contratoAtual.Itens.Select(x => new ContratoVersaoItem
                    {
                        ContratoVersaoId = idVersao,
                        OrcamentoDetalheId = x.Id,
                        Valor = x.Valor // Valor que estava valendo antes do aditivo
                    }).ToList();

                    await _versaoRepo.InserirItensAsync(itensHistoricos);
                }

                // 4. PREPARAR A ATUALIZAÇÃO DO CONTRATO PRINCIPAL (O FUTURO)
                // Agora pegamos o ViewModel do Aditivo (que tem a grid editada) e aplicamos no Contrato
                
                // Recalcula o Valor Total Global baseado nos novos itens da grid
                decimal novoValorTotal = 0;
                if (vm.Itens != null) novoValorTotal = vm.Itens.Sum(x => x.Valor);

                var contratoParaAtualizar = new ContratoViewModel
                {
                    Id = vm.ContratoId,
                    
                    // Campos que mudam com o aditivo:
                    ObjetoContrato = !string.IsNullOrWhiteSpace(vm.NovoObjeto) ? vm.NovoObjeto : contratoAtual.ObjetoContrato,
                    // Data Inicio geralmente mantém a original do contrato, a não ser que seja renegociação total. 
                    // Vamos manter a original:
                    DataInicio = contratoAtual.DataInicio, 
                    // Nova data fim se informada, senão mantém a atual
                    DataFim = vm.NovaDataFim ?? contratoAtual.DataFim, 
                    ValorContrato = novoValorTotal, // O novo total vem da soma dos itens editados
                    Observacao = vm.Justificativa, // Atualiza obs do contrato atual
                    
                    // Campos fixos (mantém o original):
                    FornecedorIdCompleto = contratoAtual.FornecedorIdCompleto,
                    NumeroContrato = contratoAtual.NumeroContrato,
                    AnoContrato = contratoAtual.AnoContrato,
                    DataAssinatura = contratoAtual.DataAssinatura,
                    OrcamentoId = contratoAtual.OrcamentoId, // Importante manter o vínculo
                    Ativo = true,
                    
                    // NOVOS ITENS (A grid editada pelo usuário)
                    Itens = vm.Itens ?? new List<ContratoItemViewModel>()
                };

                // 5. EFETIVAR A ATUALIZAÇÃO NO BANCO
                // O método AtualizarAsync do repositório já sabe: limpa ContratoItem antigo e insere o novo
                await _contratoRepo.AtualizarAsync(contratoParaAtualizar);

                // Confirma a transação
                scope.Complete();
            }
        }

        // --- Sincronização em caso de edição manual (sem aditivo) ---
        public async Task AtualizarSnapshotUltimaVersaoAsync(int contratoId)
        {
            var versaoAtual = await _versaoRepo.ObterVersaoAtualAsync(contratoId);
            if (versaoAtual == null) return;

            var contratoVigente = await _contratoRepo.ObterParaEdicaoAsync(contratoId);
            if(contratoVigente == null) return;

            // Atualiza Header
            versaoAtual.DataInicio = contratoVigente.DataInicio;
            versaoAtual.DataFim = contratoVigente.DataFim;
            versaoAtual.ValorContrato = contratoVigente.ValorContrato;
            await _versaoRepo.AtualizarAsync(versaoAtual);

            // Atualiza Itens (Remove e Recria)
            await _versaoRepo.ExcluirItensPorVersaoAsync(versaoAtual.Id);

            if (contratoVigente.Itens != null && contratoVigente.Itens.Any())
            {
                var novosItensSnapshot = contratoVigente.Itens.Select(x => new ContratoVersaoItem
                {
                    ContratoVersaoId = versaoAtual.Id,
                    OrcamentoDetalheId = x.Id,
                    Valor = x.Valor
                }).ToList();

                await _versaoRepo.InserirItensAsync(novosItensSnapshot);
            }
        }

        // --- CANCELAR ADITIVO (ROLLBACK) ---
        public async Task<(ContratoVersao Removida, ContratoVersao? Vigente)> CancelarUltimoAditivoAsync(
            int contratoId, int versao, string justificativa)
        {
            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var versaoParaRemover = await _versaoRepo.ObterPorIdAsync(contratoId, versao);
                if (versaoParaRemover == null) throw new ArgumentException("Versão não encontrada.");

                var ultimaVersao = await _versaoRepo.ObterVersaoAtualAsync(contratoId);
                if (ultimaVersao.Id != versaoParaRemover.Id) throw new ArgumentException("Apenas o último aditivo pode ser cancelado.");

                // A versão que estamos removendo contém o SNAPSHOT de como o contrato era ANTES desse aditivo.
                // Então, para cancelar o aditivo, nós restauramos os dados dessa versão para a tabela principal.

                // 1. Recupera os itens dessa versão histórica (Backup)
                var itensHistoricos = await _versaoRepo.ListarItensPorVersaoAsync(versaoParaRemover.Id);

                // 2. Monta o objeto para restaurar o contrato principal
                // Precisamos de alguns dados do contrato atual (fixos) para montar o ViewModel completo
                var contratoAtual = await _contratoRepo.ObterParaEdicaoAsync(contratoId);
                
                var contratoRestaurado = new ContratoViewModel
                {
                    Id = contratoId,
                    // Restauramos os dados que estavam salvos no histórico
                    ObjetoContrato = versaoParaRemover.ObjetoContrato,
                    DataInicio = versaoParaRemover.DataInicio,
                    DataFim = versaoParaRemover.DataFim,
                    ValorContrato = versaoParaRemover.ValorContrato,
                    
                    // Mantemos dados fixos
                    FornecedorIdCompleto = contratoAtual.FornecedorIdCompleto,
                    NumeroContrato = contratoAtual.NumeroContrato,
                    AnoContrato = contratoAtual.AnoContrato,
                    DataAssinatura = contratoAtual.DataAssinatura,
                    OrcamentoId = contratoAtual.OrcamentoId,
                    Ativo = true,
                    Observacao = $"Aditivo cancelado. Restaurado para estado anterior (v.{versao-1}). Justificativa: {justificativa}",

                    // Restaura os itens
                    Itens = itensHistoricos.Select(x => new ContratoItemViewModel 
                    {
                        Id = x.OrcamentoDetalheId,
                        Valor = x.Valor,
                        NomeItem = x.NomeItem // Apenas visual, mas o repo usa ID
                    }).ToList()
                };

                // 3. Atualiza o Contrato Principal (Rollback dos dados e itens)
                await _contratoRepo.AtualizarAsync(contratoRestaurado);

                // 4. Remove o registro do histórico
                await _versaoRepo.ExcluirAsync(versaoParaRemover.Id);

                scope.Complete();

                return (versaoParaRemover, null);
            }
        }
    }
}