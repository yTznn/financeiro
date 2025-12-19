using Financeiro.Models;
using Financeiro.Models.ViewModels;
using Financeiro.Repositorios;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

            // 2. Snapshot dos ITENS (Antes eram Naturezas)
            // Se o ViewModel já tem os itens preenchidos (geralmente tem no Insert), usamos eles.
            if (vm.Itens != null && vm.Itens.Any())
            {
                var itensHistorico = vm.Itens.Select(item => new ContratoVersaoItem
                {
                    ContratoVersaoId = versaoId,
                    OrcamentoDetalheId = item.Id, // No VM, Id = OrcamentoDetalheId
                    Valor = item.Valor
                }).ToList();

                await _versaoRepo.InserirItensAsync(itensHistorico);
            }
        }

        // --- GERAÇÃO DE ADITIVO (SNAPSHOT V-NEXT) ---
        public async Task CriarAditivoAsync(AditivoContratoViewModel vm)
        {
            if (vm == null) throw new ArgumentNullException(nameof(vm));
            
            var contrato = await _contratoRepo.ObterParaEdicaoAsync(vm.ContratoId)
                           ?? throw new ArgumentException("Contrato não encontrado.");

            // 1. Garante V1 se não existir
            var vigente = await _versaoRepo.ObterVersaoAtualAsync(vm.ContratoId);
            if (vigente == null)
            {
                await CriarVersaoInicialAsync(contrato);
                vigente = await _versaoRepo.ObterVersaoAtualAsync(vm.ContratoId);
            }

            // 2. Define novos dados (Datas)
            DateTime novaIni = vm.DataInicioAditivo ?? vigente.DataInicio;
            DateTime novaFim = vm.NovaDataFim ?? vigente.DataFim;

            // 3. Define novo Valor
            decimal valorFinal = vigente.ValorContrato;
            bool alteraValor = vm.TipoAditivo == TipoAditivo.Acrescimo || 
                               vm.TipoAditivo == TipoAditivo.Supressao ||
                               vm.TipoAditivo == TipoAditivo.PrazoAcrescimo ||
                               vm.TipoAditivo == TipoAditivo.PrazoSupressao;

            if (alteraValor)
            {
                if (vm.NovoValorDecimal == 0) throw new ArgumentException("Informe um valor diferente de zero.");
                decimal delta = Math.Abs(vm.NovoValorDecimal);
                
                if (vm.TipoAditivo == TipoAditivo.Supressao || vm.TipoAditivo == TipoAditivo.PrazoSupressao)
                    delta = -delta;

                if (vm.EhValorMensal)
                {
                    int meses = CalcularMeses(novaIni, novaFim);
                    delta = delta * meses;
                }
                valorFinal = vigente.ValorContrato + delta;
                if (valorFinal < 0) throw new ArgumentException("O valor do contrato não pode ficar negativo.");
            }

            // 4. Criação da Nova Versão (Header do Histórico)
            var novaVersao = new ContratoVersao
            {
                ContratoId = vm.ContratoId,
                Versao = vigente.Versao + 1,
                DataInicio = novaIni,
                DataFim = novaFim,
                ValorContrato = valorFinal,
                ObjetoContrato = contrato.ObjetoContrato,
                TipoAditivo = vm.TipoAditivo,
                Observacao = vm.Justificativa,
                DataRegistro = DateTime.Now,
                DataInicioAditivo = vm.DataInicioAditivo,
                Ativo = true
            };

            int novaVersaoId = await _versaoRepo.InserirAsync(novaVersao);

            // 5. Snapshot dos ITENS (Cópia da versão anterior/atual)
            // Precisamos pegar os itens que estão VIGENTES no banco agora
            var itensAtuais = await _contratoRepo.ListarItensPorContratoAsync(vm.ContratoId);
            
            if (itensAtuais != null)
            {
                var itensCopia = new List<ContratoVersaoItem>();
                foreach(var item in itensAtuais)
                {
                    // O retorno é dynamic, precisamos converter
                    // Propriedades vindas do Repo: Id (OrcamentoDetalheId), Nome, ValorTotalItemNoContrato
                    itensCopia.Add(new ContratoVersaoItem
                    {
                        ContratoVersaoId = novaVersaoId,
                        OrcamentoDetalheId = (int)item.Id, 
                        Valor = (decimal)item.ValorTotalItemNoContrato
                    });
                }

                if (itensCopia.Any())
                {
                    await _versaoRepo.InserirItensAsync(itensCopia);
                }
            }

            // 6. Atualiza a tabela PAI (Contrato) IMEDIATAMENTE
            // Isso faz com que o contrato fique com o Valor Novo, mas os Itens Antigos.
            // A View vai detectar isso e avisar: "Redistribua o rateio".
            await _contratoRepo.AtualizarVigenciaEValorAsync(
                vm.ContratoId, 
                novaIni, 
                novaFim, 
                valorFinal
            );
        }

        // --- ATUALIZAÇÃO DE SNAPSHOT (CORREÇÃO DE RATEIO) ---
        public async Task AtualizarSnapshotUltimaVersaoAsync(int contratoId)
        {
            // 1. Pega a última versão
            var versaoAtual = await _versaoRepo.ObterVersaoAtualAsync(contratoId);
            if (versaoAtual == null) return;

            // 2. Atualiza Header da versão (caso data/valor tenham mudado na edição)
            var contratoVigente = await _contratoRepo.ObterParaEdicaoAsync(contratoId);
            if(contratoVigente == null) return;

            versaoAtual.DataInicio = contratoVigente.DataInicio;
            versaoAtual.DataFim = contratoVigente.DataFim;
            versaoAtual.ValorContrato = contratoVigente.ValorContrato;
            await _versaoRepo.AtualizarAsync(versaoAtual);

            // 3. Atualiza os ITENS da versão (Snapshot) para ficarem iguais aos do Contrato
            // Primeiro limpa os itens antigos dessa versão
            await _versaoRepo.ExcluirItensPorVersaoAsync(versaoAtual.Id);

            // Agora insere os novos baseados no contrato vigente
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

        public async Task<(ContratoVersao Removida, ContratoVersao? Vigente)> CancelarUltimoAditivoAsync(
            int contratoId, int versao, string justificativa)
        {
            var atual = await _versaoRepo.ObterVersaoAtualAsync(contratoId)
                        ?? throw new ArgumentException("Não há versão vigente para cancelar.");

            if (atual.Versao != versao)
                throw new ArgumentException("Versão informada não é a vigente.");

            // Remove a versão atual (aditivo cancelado)
            await _versaoRepo.ExcluirAsync(atual.Id);

            // Busca a versão anterior (que vai voltar a ser a vigente)
            var anterior = await _versaoRepo.ObterVersaoAtualAsync(contratoId);

            if (anterior != null)
            {
                // 1. Restaura Header (Datas e Valor Total)
                await _contratoRepo.AtualizarVigenciaEValorAsync(
                    contratoId,
                    anterior.DataInicio,
                    anterior.DataFim,
                    anterior.ValorContrato
                );

                // 2. Restaura os ITENS (Deleta os itens atuais do contrato e insere os da versão anterior)
                var itensHistoricoAnterior = await _versaoRepo.ListarItensPorVersaoAsync(anterior.Id);
                
                // Precisamos converter para ViewModel para usar o método AtualizarAsync do Repositório,
                // ou atualizar manualmente. Vamos montar um ViewModel para facilitar e usar a lógica de INSERT/DELETE que já existe lá.
                var contratoRestaurado = await _contratoRepo.ObterParaEdicaoAsync(contratoId);
                
                if (contratoRestaurado != null)
                {
                    contratoRestaurado.Itens = itensHistoricoAnterior.Select(h => new ContratoItemViewModel
                    {
                        Id = h.OrcamentoDetalheId,
                        Valor = h.Valor
                        // Nome não é necessário para o Insert
                    }).ToList();

                    await _contratoRepo.AtualizarAsync(contratoRestaurado);
                }
            }

            return (atual, anterior);
        }

        private int CalcularMeses(DateTime inicio, DateTime fim)
        {
            if (fim < inicio) return 0;
            return ((fim.Year - inicio.Year) * 12) + fim.Month - inicio.Month + 1;
        }
    }
}