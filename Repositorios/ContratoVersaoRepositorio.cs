using Dapper;
using Financeiro.Infraestrutura;
using Financeiro.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Financeiro.Repositorios
{
    public class ContratoVersaoRepositorio : IContratoVersaoRepositorio
    {
        private readonly IDbConnectionFactory _factory;

        public ContratoVersaoRepositorio(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task InserirAsync(ContratoVersao versao)
        {
            const string sql = @"
                INSERT INTO ContratoVersao 
                    (ContratoId, Versao, ObjetoContrato, DataInicio, DataFim, ValorContrato, TipoAditivo, Observacao, DataRegistro, DataInicioAditivo)
                VALUES 
                    (@ContratoId, @Versao, @ObjetoContrato, @DataInicio, @DataFim, @ValorContrato, @TipoAditivo, @Observacao, @DataRegistro, @DataInicioAditivo);";

            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(sql, versao);
        }

        public async Task<ContratoVersao?> ObterVersaoAtualAsync(int contratoId)
        {
            const string sql = @"
                SELECT TOP 1 * FROM ContratoVersao
                WHERE ContratoId = @contratoId
                ORDER BY Versao DESC;";

            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<ContratoVersao>(sql, new { contratoId });
        }

        public async Task<IEnumerable<ContratoVersao>> ListarPorContratoAsync(int contratoId)
        {
            const string sql = @"
                SELECT * FROM ContratoVersao
                WHERE ContratoId = @contratoId
                ORDER BY Versao DESC;";

            using var conn = _factory.CreateConnection();
            return await conn.QueryAsync<ContratoVersao>(sql, new { contratoId });
        }

        public async Task<(IEnumerable<ContratoVersao> Itens, int TotalPaginas)> ListarPaginadoAsync(int contratoId, int pagina, int tamanhoPagina = 5)
        {
            const string sqlItens = @"
                SELECT * FROM ContratoVersao
                WHERE ContratoId = @contratoId
                ORDER BY Versao DESC
                OFFSET @Offset ROWS FETCH NEXT @TamanhoPagina ROWS ONLY;";

            const string sqlCount = @"
                SELECT COUNT(*) FROM ContratoVersao
                WHERE ContratoId = @contratoId;";

            using var conn = _factory.CreateConnection();
            var itens = await conn.QueryAsync<ContratoVersao>(sqlItens, new
            {
                contratoId,
                Offset = (pagina - 1) * tamanhoPagina,
                TamanhoPagina = tamanhoPagina
            });

            var totalItens   = await conn.ExecuteScalarAsync<int>(sqlCount, new { contratoId });
            var totalPaginas = (int)Math.Ceiling(totalItens / (double)tamanhoPagina);

            return (itens, totalPaginas);
        }

        // ✅ Quantidade total de aditivos de um contrato
        public async Task<int> ContarPorContratoAsync(int contratoId)
        {
            const string sql = "SELECT COUNT(*) FROM ContratoVersao WHERE ContratoId = @contratoId;";
            using var conn = _factory.CreateConnection();
            return await conn.ExecuteScalarAsync<int>(sql, new { contratoId });
        }

        // ✅ Exclui a versão (aditivo)
        public async Task ExcluirAsync(int versaoId)
        {
            const string sql = "DELETE FROM ContratoVersao WHERE Id = @versaoId;";
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(sql, new { versaoId });
        }

        // ✅ Restaura os campos do contrato com base na versão anterior
        public async Task RestaurarContratoAPartirDaVersaoAsync(ContratoVersao versaoAnterior)
        {
            const string sql = @"
                UPDATE Contrato
                SET  ValorContrato  = @ValorContrato,
                     DataFim        = @DataFim,
                     ObjetoContrato = @ObjetoContrato
                WHERE Id = @ContratoId;";

            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(sql, new
            {
                versaoAnterior.ValorContrato,
                versaoAnterior.DataFim,
                versaoAnterior.ObjetoContrato,
                versaoAnterior.ContratoId
            });
        }
    }
}