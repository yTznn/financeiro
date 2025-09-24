using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Financeiro.Infraestrutura;
using Financeiro.Models;

namespace Financeiro.Repositorios
{
    /// <summary>
    /// Repositório de versões/aditivos do Instrumento.
    /// Mapeia dbo.InstrumentoVersao (InstrumentoId).
    /// </summary>
    public class InstrumentoVersaoRepositorio : IInstrumentoVersaoRepositorio
    {
        private readonly IDbConnectionFactory _factory;

        public InstrumentoVersaoRepositorio(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task InserirAsync(InstrumentoVersao versao)
        {
            const string sql = @"
INSERT INTO dbo.InstrumentoVersao
    (InstrumentoId, Versao, VigenciaInicio, VigenciaFim,
     Valor, Objeto, TipoAditivo, Observacao, DataAssinatura, DataRegistro)
VALUES
    (@InstrumentoId, @Versao, @VigenciaInicio, @VigenciaFim,
     @Valor, @Objeto, @TipoAditivo, @Observacao, @DataAssinatura, @DataRegistro);";

            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(sql, versao);
        }

        public async Task<IEnumerable<InstrumentoVersao>> ListarPorInstrumentoAsync(int instrumentoId)
        {
            const string sql = @"
SELECT *
FROM dbo.InstrumentoVersao
WHERE InstrumentoId = @instrumentoId
ORDER BY Versao DESC;";

            using var conn = _factory.CreateConnection();
            return await conn.QueryAsync<InstrumentoVersao>(sql, new { instrumentoId });
        }

        public async Task<InstrumentoVersao?> ObterVersaoAtualAsync(int instrumentoId)
        {
            const string sql = @"
SELECT TOP 1 *
FROM dbo.InstrumentoVersao
WHERE InstrumentoId = @instrumentoId
  AND VigenciaFim IS NULL
ORDER BY Versao DESC;";

            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<InstrumentoVersao>(sql, new { instrumentoId });
        }

        public async Task<(IEnumerable<InstrumentoVersao> Itens, int TotalPaginas)> ListarPaginadoAsync(int instrumentoId, int pagina, int tamPag = 5)
        {
            const string sqlItens = @"
SELECT *
FROM dbo.InstrumentoVersao
WHERE InstrumentoId = @instrumentoId
ORDER BY Versao DESC
OFFSET (@pagina-1) * @tam ROWS FETCH NEXT @tam ROWS ONLY;";

            const string sqlCount = @"
SELECT COUNT(*)
FROM dbo.InstrumentoVersao
WHERE InstrumentoId = @instrumentoId;";

            using var conn = _factory.CreateConnection();
            var itens = await conn.QueryAsync<InstrumentoVersao>(sqlItens, new { instrumentoId, pagina, tam = tamPag });
            int total = await conn.ExecuteScalarAsync<int>(sqlCount, new { instrumentoId });
            int totalPaginas = (int)Math.Ceiling(total / (double)tamPag);
            return (itens, totalPaginas);
        }

        public async Task ExcluirAsync(int versaoId)
        {
            const string sql = @"DELETE FROM dbo.InstrumentoVersao WHERE Id = @versaoId;";
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(sql, new { versaoId });
        }

        public async Task<InstrumentoVersao?> ObterVersaoAnteriorAsync(int instrumentoId, int versaoAtual)
        {
            const string sql = @"
SELECT TOP 1 *
FROM dbo.InstrumentoVersao
WHERE InstrumentoId = @instrumentoId
  AND Versao < @versaoAtual
ORDER BY Versao DESC;";
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<InstrumentoVersao>(sql, new { instrumentoId, versaoAtual });
        }

        public async Task AtualizarVigenciaFimAsync(int versaoId, DateTime? dataFim)
        {
            const string sql = @"UPDATE dbo.InstrumentoVersao SET VigenciaFim = @dataFim WHERE Id = @versaoId;";
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(sql, new { versaoId, dataFim });
        }

        public async Task AtualizarDetalhesAsync(int versaoId, decimal novoValor, TipoAditivo tipoAditivo, string? observacao, DateTime? dataAssinatura)
        {
            const string sql = @"
UPDATE dbo.InstrumentoVersao
SET Valor = @novoValor,
    TipoAditivo = @tipoAditivo,
    Observacao = @observacao,
    DataAssinatura = @dataAssinatura
WHERE Id = @versaoId;";
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(sql, new { versaoId, novoValor, tipoAditivo, observacao, dataAssinatura });
        }
    }
}