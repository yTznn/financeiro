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
    /// Mantém compatibilidade com o schema atual (dbo.AcordoVersao / TipoAcordoId).
    /// </summary>
    public class InstrumentoVersaoRepositorio : IInstrumentoVersaoRepositorio
    {
        private readonly IDbConnectionFactory _factory;

        public InstrumentoVersaoRepositorio(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task InserirAsync(AcordoVersao versao)
        {
            const string sql = @"
INSERT INTO dbo.AcordoVersao
    (TipoAcordoId, Versao, VigenciaInicio, VigenciaFim,
     Valor, Objeto, TipoAditivo, Observacao, DataAssinatura, DataRegistro)
VALUES
    (@TipoAcordoId, @Versao, @VigenciaInicio, @VigenciaFim,
     @Valor, @Objeto, @TipoAditivo, @Observacao, @DataAssinatura, @DataRegistro);";

            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(sql, versao);
        }

        public async Task<IEnumerable<AcordoVersao>> ListarPorInstrumentoAsync(int instrumentoId)
        {
            const string sql = @"
SELECT *
FROM dbo.AcordoVersao
WHERE TipoAcordoId = @instrumentoId
ORDER BY Versao DESC;";

            using var conn = _factory.CreateConnection();
            return await conn.QueryAsync<AcordoVersao>(sql, new { instrumentoId });
        }

        public async Task<AcordoVersao?> ObterVersaoAtualAsync(int instrumentoId)
        {
            const string sql = @"
SELECT TOP 1 *
FROM dbo.AcordoVersao
WHERE TipoAcordoId = @instrumentoId
  AND VigenciaFim IS NULL
ORDER BY Versao DESC;";

            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<AcordoVersao>(sql, new { instrumentoId });
        }

        public async Task<(IEnumerable<AcordoVersao> Itens, int TotalPaginas)> ListarPaginadoAsync(int instrumentoId, int pagina, int tamPag = 5)
        {
            const string sqlItens = @"
SELECT *
FROM dbo.AcordoVersao
WHERE TipoAcordoId = @instrumentoId
ORDER BY Versao DESC
OFFSET (@pagina-1) * @tam ROWS FETCH NEXT @tam ROWS ONLY;";

            const string sqlCount = @"
SELECT COUNT(*)
FROM dbo.AcordoVersao
WHERE TipoAcordoId = @instrumentoId;";

            using var conn = _factory.CreateConnection();
            var itens = await conn.QueryAsync<AcordoVersao>(sqlItens, new { instrumentoId, pagina, tam = tamPag });
            int total = await conn.ExecuteScalarAsync<int>(sqlCount, new { instrumentoId });
            int totalPaginas = (int)Math.Ceiling(total / (double)tamPag);
            return (itens, totalPaginas);
        }

        public async Task ExcluirAsync(int versaoId)
        {
            const string sql = @"DELETE FROM dbo.AcordoVersao WHERE Id = @versaoId;";
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(sql, new { versaoId });
        }
        public async Task<AcordoVersao?> ObterVersaoAnteriorAsync(int instrumentoId, int versaoAtual)
        {
            const string sql = @"
        SELECT TOP 1 *
        FROM dbo.AcordoVersao
        WHERE TipoAcordoId = @instrumentoId
        AND Versao < @versaoAtual
        ORDER BY Versao DESC;";
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<AcordoVersao>(sql, new { instrumentoId, versaoAtual });
        }
        public async Task AtualizarVigenciaFimAsync(int versaoId, DateTime? dataFim)
        {
            const string sql = @"UPDATE dbo.AcordoVersao SET VigenciaFim = @dataFim WHERE Id = @versaoId;";
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(sql, new { versaoId, dataFim });
        }
        
        // --- NOVO MÉTODO ADICIONADO AQUI ---
        public async Task AtualizarDetalhesAsync(int versaoId, decimal novoValor, TipoAditivo tipoAditivo, string? observacao, DateTime? dataAssinatura)
        {
            const string sql = @"
                UPDATE dbo.AcordoVersao 
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