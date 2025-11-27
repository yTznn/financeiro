using Dapper;
using Financeiro.Infraestrutura;
using Financeiro.Models;
using Financeiro.Models.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Globalization;

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
                    INSERT INTO MovimentacaoFinanceira (DataMovimentacao, FornecedorIdCompleto, ValorTotal, Historico, Ativo)
                    VALUES (@DataMovimentacao, @FornecedorIdCompleto, @ValorTotal, @Historico, 1);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                int movId = await conn.QuerySingleAsync<int>(sqlHeader, new
                {
                    vm.DataMovimentacao,
                    vm.FornecedorIdCompleto,
                    ValorTotal = vm.ValorTotalDecimal, 
                    vm.Historico
                }, tx);

                const string sqlItem = @"
                    INSERT INTO MovimentacaoRateio (MovimentacaoId, InstrumentoId, ContratoId, NaturezaId, Valor)
                    VALUES (@MovimentacaoId, @InstrumentoId, @ContratoId, @NaturezaId, @Valor);";

                if (vm.Rateios != null)
                {
                    foreach (var item in vm.Rateios)
                    {
                        await conn.ExecuteAsync(sqlItem, new
                        {
                            MovimentacaoId = movId,
                            item.InstrumentoId,
                            item.ContratoId,
                            item.NaturezaId,
                            Valor = item.ValorDecimal
                        }, tx);
                    }
                }

                tx.Commit();
                return movId;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public async Task<MovimentacaoViewModel?> ObterCompletoPorIdAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            
            // 1. Busca Cabeçalho
            const string sqlHeader = "SELECT * FROM MovimentacaoFinanceira WHERE Id = @id";
            var mov = await conn.QuerySingleOrDefaultAsync<MovimentacaoFinanceira>(sqlHeader, new { id });

            if (mov == null) return null;

            // 2. Busca Rateios
            const string sqlRateios = @"
                SELECT * FROM MovimentacaoRateio 
                WHERE MovimentacaoId = @id";
            
            var rateios = await conn.QueryAsync<MovimentacaoRateioViewModel>(sqlRateios, new { id });

            var ptBR = new CultureInfo("pt-BR");

            // 3. Monta o ViewModel
            // AQUI O PULO DO GATO: Se mov.ValorTotal for decimal, converte.
            // Se o erro persiste, pode ser que mov.ValorTotal seja string na classe MovimentacaoFinanceira?
            // Vou forçar um cast para decimal para garantir.
            decimal vTotal = Convert.ToDecimal(mov.ValorTotal);

            return new MovimentacaoViewModel
            {
                Id = mov.Id,
                DataMovimentacao = mov.DataMovimentacao,
                FornecedorIdCompleto = mov.FornecedorIdCompleto,
                ValorTotal = vTotal.ToString("N2", ptBR), 
                Historico = mov.Historico,
                Rateios = rateios.Select(r => 
                {
                     // Mesma coisa aqui: garante que é decimal antes de formatar
                     decimal vRateio = Convert.ToDecimal(r.Valor);
                     return new MovimentacaoRateioViewModel
                     {
                        InstrumentoId = r.InstrumentoId,
                        ContratoId = r.ContratoId,
                        NaturezaId = r.NaturezaId,
                        Valor = vRateio.ToString("N2", ptBR)
                     };
                }).ToList()
            };
        }
    }
}