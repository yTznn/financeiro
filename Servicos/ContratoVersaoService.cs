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
                DataInicioAditivo = null
            };

            int versaoId = await _versaoRepo.InserirAsync(versaoInicial);

            // 2. Snapshot das Naturezas
            var naturezasAtuais = await _contratoRepo.ListarNaturezasPorContratoAsync(vm.Id);
            
            if (naturezasAtuais.Any())
            {
                var itensHistorico = naturezasAtuais.Select(n => new ContratoVersaoNatureza
                {
                    ContratoVersaoId = versaoId,
                    NaturezaId = n.NaturezaId,
                    Valor = n.Valor
                });

                await _versaoRepo.InserirNaturezasHistoricoAsync(versaoId, itensHistorico);
            }
        }

        // --- GERAÇÃO DE ADITIVO (SNAPSHOT V-NEXT) ---
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
                DataInicioAditivo = vm.DataInicioAditivo
            };

            int novaVersaoId = await _versaoRepo.InserirAsync(novaVersao);

            // 5. Snapshot das Naturezas (Cópia da versão anterior)
            // O valor total da versão será diferente da soma destas naturezas copiadas até o usuário editar na próxima tela.
            var naturezasBase = await _contratoRepo.ListarNaturezasPorContratoAsync(vm.ContratoId);
            
            if (naturezasBase.Any())
            {
                var itensCopia = naturezasBase.Select(n => new ContratoVersaoNatureza
                {
                    ContratoVersaoId = novaVersaoId,
                    NaturezaId = n.NaturezaId,
                    Valor = n.Valor
                });

                await _versaoRepo.InserirNaturezasHistoricoAsync(novaVersaoId, itensCopia);
            }

            // [CORREÇÃO CRÍTICA AQUI]
            // Atualiza a tabela PAI (Contrato) IMEDIATAMENTE para que a tela de Edição já abra com os dados novos!
            // Assim, o View vai detectar a diferença entre o Valor Total (Novo) e as Naturezas (Antigas) e pedir o rateio.
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

            // 2. Pega o rateio OFICIAL (que o usuário acabou de salvar)
            var naturezasOficiais = await _contratoRepo.ListarNaturezasPorContratoAsync(contratoId);

            if (naturezasOficiais.Any())
            {
                // 3. Converte para o formato de histórico
                var novosItensHistorico = naturezasOficiais.Select(n => new ContratoVersaoNatureza
                {
                    ContratoVersaoId = versaoAtual.Id,
                    NaturezaId = n.NaturezaId,
                    Valor = n.Valor
                });

                // 4. Atualiza o Snapshot para ficar igual ao Oficial
                await _versaoRepo.RecriarNaturezasHistoricoAsync(versaoAtual.Id, novosItensHistorico);
            }
        }

        public async Task<(ContratoVersao removida, ContratoVersao? vigente)> CancelarUltimoAditivoAsync(
            int contratoId, int versao, string justificativa)
        {
            var atual = await _versaoRepo.ObterVersaoAtualAsync(contratoId)
                        ?? throw new ArgumentException("Não há versão vigente para cancelar.");

            if (atual.Versao != versao)
                throw new ArgumentException("Versão informada não é a vigente.");

            // Remove a vigente
            await _versaoRepo.ExcluirAsync(atual.Id);

            var anterior = await _versaoRepo.ObterVersaoAtualAsync(contratoId);

            if (anterior != null)
            {
                // Restaura o Contrato PAI + Naturezas PAI a partir do histórico anterior
                await _versaoRepo.RestaurarContratoAPartirDaVersaoAsync(anterior);
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