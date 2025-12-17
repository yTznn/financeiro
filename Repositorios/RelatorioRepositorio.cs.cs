using Dapper;
using Financeiro.Infraestrutura;
using Financeiro.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Financeiro.Repositorios
{
    public class RelatorioRepositorio : IRelatorioRepositorio
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public RelatorioRepositorio(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<RelatorioInstrumentoViewModel> GerarLancamentosXRecebimentosAsync(
            int instrumentoId, 
            DateTime dataInicio, 
            DateTime dataFim)
        {
            // O SQL trará o Instrumento, Recebimentos (agregado) e Movimentações (lista).
            const string sql = @"
                -- 1. Detalhes do Instrumento e Saldo ANTERIOR ao período
                SELECT 
                    Id, Numero, Saldo 
                FROM Instrumento 
                WHERE Id = @InstrumentoId;

                -- 2. Soma total de RECEBIMENTOS no período (para regra de coloração)
                SELECT 
                    ISNULL(SUM(Valor), 0) 
                FROM RecebimentoInstrumento -- Tabela correta do seu DB
                WHERE InstrumentoId = @InstrumentoId 
                  AND DataInicio >= @DataInicio 
                  AND DataFim <= @DataFim;

                -- 3. Movimentações Financeiras (Lançamentos/SAÍDAS) no período, ordenadas por data
                SELECT 
                    m.Id, 
                    m.ValorTotal AS ValorLancamento, 
                    m.DataMovimentacao AS DataLancamento
                FROM MovimentacaoFinanceira m
                INNER JOIN MovimentacaoRateio mr ON m.Id = mr.MovimentacaoId
                WHERE mr.InstrumentoId = @InstrumentoId
                  AND m.DataMovimentacao >= @DataInicio
                  AND m.DataMovimentacao <= @DataFim
                  AND m.Ativo = 1 -- Assumindo que a movimentação inativa não conta
                ORDER BY m.DataMovimentacao ASC, m.Id ASC;
            ";

            using var conn = _connectionFactory.CreateConnection();
            using var multi = await conn.QueryMultipleAsync(sql, new 
            { 
                InstrumentoId = instrumentoId, 
                DataInicio = dataInicio, 
                DataFim = dataFim
            });

            // 1. Processar Instrumento
            var instrumento = await multi.ReadFirstOrDefaultAsync<dynamic>();
            if (instrumento == null) return null;

            // 2. Processar Soma de Recebimentos
            var somaRecebimentos = await multi.ReadFirstAsync<decimal>();

            // 3. Processar Lançamentos (Itens da Tabela)
            var lancamentosRaw = await multi.ReadAsync<LancamentoDetalheViewModel>();
            var lancamentos = lancamentosRaw.ToList();

            // ==========================================================
            // CÁLCULO CUMULATIVO E MONTAGEM DA VIEWMODEL (C#)
            // ==========================================================
            
            var resultado = new RelatorioInstrumentoViewModel
            {
                InstrumentoNumero = instrumento.Numero,
                SaldoInicialMes = instrumento.Saldo, // Saldo inicial é o Saldo atual do Instrumento (simplificado)
                PossuiRecebimento = somaRecebimentos > 0,
                DataInicioPeriodo = dataInicio,
                DataFimPeriodo = dataFim,
                ReferenciaPeriodo = $"{dataInicio:MMM}/{dataInicio:yy}", // Ex: Jan/25
                ValorTotalSaidas = lancamentos.Sum(l => l.ValorLancamento),
                
                // Inicializa a lista de detalhes
                Lancamentos = new List<LancamentoDetalheViewModel>()
            };

            decimal saldoAtual = resultado.SaldoInicialMes;
            int contador = 1;

            foreach (var lancamento in lancamentos)
            {
                // Calcula o saldo após este lançamento
                saldoAtual -= lancamento.ValorLancamento; 

                // Adiciona o item processado
                resultado.Lancamentos.Add(new LancamentoDetalheViewModel
                {
                    LancamentoSequencial = contador++,
                    DataLancamento = lancamento.DataLancamento,
                    ValorLancamento = lancamento.ValorLancamento,
                    SaldoCumulativo = saldoAtual // Saldo após a saída
                });
            }

            // O saldo final é o saldo restante após todos os lançamentos
            resultado.SaldoTotalFinal = saldoAtual;

            return resultado;
        }
    }
}