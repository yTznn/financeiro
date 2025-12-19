using Dapper;
using Financeiro.Infraestrutura;
using Financeiro.Models;
using Financeiro.Models.ViewModels;
using System.Globalization;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace Financeiro.Repositorios
{
    public class MovimentacaoRepositorio : IMovimentacaoRepositorio
    {
        private readonly IDbConnectionFactory _factory;

        public MovimentacaoRepositorio(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<IEnumerable<MovimentacaoFinanceira>> ListarAsync()
        {
            const string sql = "SELECT * FROM MovimentacaoFinanceira ORDER BY DataMovimentacao DESC";
            using var conn = _factory.CreateConnection();
            return await conn.QueryAsync<MovimentacaoFinanceira>(sql);
        }

        public async Task<int> InserirAsync(MovimentacaoViewModel vm)
        {
            using var conn = _factory.CreateConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                const string sqlHeader = @"
                    INSERT INTO MovimentacaoFinanceira 
                        (DataMovimentacao, FornecedorIdCompleto, ValorTotal, Historico, Ativo, DataReferenciaInicio, DataReferenciaFim, EhLancamentoAvulso, JustificativaAvulso)
                    VALUES 
                        (@DataMovimentacao, @FornecedorIdCompleto, @ValorTotal, @Historico, 1, @DataReferenciaInicio, @DataReferenciaFim, 0, NULL);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                int movId = await conn.QuerySingleAsync<int>(sqlHeader, new
                {
                    vm.DataMovimentacao,
                    vm.FornecedorIdCompleto,
                    ValorTotal = vm.ValorTotalDecimal, 
                    vm.Historico,
                    vm.DataReferenciaInicio,
                    vm.DataReferenciaFim
                }, tx);

                await SalvarRateiosAsync(conn, tx, movId, vm.Rateios);

                tx.Commit();
                return movId;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        // --- NOVO: MÉTODO DE ATUALIZAÇÃO PARA EDIÇÃO ---
        public async Task AtualizarAsync(MovimentacaoViewModel vm)
        {
            using var conn = _factory.CreateConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                const string sqlHeader = @"
                    UPDATE MovimentacaoFinanceira 
                    SET DataMovimentacao = @DataMovimentacao,
                        FornecedorIdCompleto = @FornecedorIdCompleto,
                        ValorTotal = @ValorTotal,
                        Historico = @Historico,
                        DataReferenciaInicio = @DataReferenciaInicio,
                        DataReferenciaFim = @DataReferenciaFim
                    WHERE Id = @Id";

                await conn.ExecuteAsync(sqlHeader, new
                {
                    vm.Id,
                    vm.DataMovimentacao,
                    vm.FornecedorIdCompleto,
                    ValorTotal = vm.ValorTotalDecimal,
                    vm.Historico,
                    vm.DataReferenciaInicio,
                    vm.DataReferenciaFim
                }, tx);

                // Remove rateios antigos e insere novos (mais seguro para integridade)
                await conn.ExecuteAsync("DELETE FROM MovimentacaoRateio WHERE MovimentacaoId = @Id", new { vm.Id }, tx);
                
                await SalvarRateiosAsync(conn, tx, vm.Id, vm.Rateios);

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        private async Task SalvarRateiosAsync(System.Data.IDbConnection conn, System.Data.IDbTransaction tx, int movId, List<MovimentacaoRateioViewModel> rateios)
        {
            const string sqlItem = @"
                INSERT INTO MovimentacaoRateio (MovimentacaoId, InstrumentoId, ContratoId, OrcamentoDetalheId, Valor)
                VALUES (@MovimentacaoId, @InstrumentoId, @ContratoId, @OrcamentoDetalheId, @Valor);";

            if (rateios != null)
            {
                foreach (var item in rateios)
                {
                    await conn.ExecuteAsync(sqlItem, new
                    {
                        MovimentacaoId = movId,
                        item.InstrumentoId,
                        item.ContratoId,
                        item.OrcamentoDetalheId, // Novo campo correto
                        Valor = item.ValorDecimal
                    }, tx);
                }
            }
        }

        public async Task<MovimentacaoViewModel?> ObterCompletoPorIdAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            
            const string sqlHeader = "SELECT * FROM MovimentacaoFinanceira WHERE Id = @id";
            var mov = await conn.QuerySingleOrDefaultAsync<MovimentacaoFinanceira>(sqlHeader, new { id });

            if (mov == null) return null;

            // Traz o nome do Item do Orçamento (OrcamentoDetalhe) no JOIN
            const string sqlRateios = @"
                SELECT r.*, od.Nome as NomeItemOrcamento 
                FROM MovimentacaoRateio r
                LEFT JOIN OrcamentoDetalhe od ON r.OrcamentoDetalheId = od.Id
                WHERE r.MovimentacaoId = @id";
            
            var rateios = await conn.QueryAsync<dynamic>(sqlRateios, new { id });

            var ptBR = new CultureInfo("pt-BR");

            return new MovimentacaoViewModel
            {
                Id = mov.Id,
                DataMovimentacao = mov.DataMovimentacao,
                FornecedorIdCompleto = mov.FornecedorIdCompleto,
                ValorTotal = mov.ValorTotal.ToString("N2", ptBR),
                Historico = mov.Historico,
                
                // Mapeia data DB -> Visual (Mês/Ano)
                ReferenciaMesAno = mov.DataReferenciaInicio?.ToString("yyyy-MM"),
                DataReferenciaInicio = mov.DataReferenciaInicio,
                DataReferenciaFim = mov.DataReferenciaFim,

                Rateios = rateios.Select(r => new MovimentacaoRateioViewModel
                {
                    InstrumentoId = r.InstrumentoId,
                    ContratoId = r.ContratoId,
                    OrcamentoDetalheId = r.OrcamentoDetalheId,
                    NomeItemOrcamento = r.NomeItemOrcamento,
                    Valor = ((decimal)r.Valor).ToString("N2", ptBR)
                }).ToList()
            };
        }

        public async Task ExcluirAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                await conn.ExecuteAsync("DELETE FROM MovimentacaoRateio WHERE MovimentacaoId = @id", new { id }, tx);
                await conn.ExecuteAsync("DELETE FROM MovimentacaoFinanceira WHERE Id = @id", new { id }, tx);
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }
    }
}