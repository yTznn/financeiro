using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Financeiro.Infraestrutura;
using Financeiro.Models;

namespace Financeiro.Repositorios
{
    /// <summary>
    /// Acesso a dados da tabela dbo.AcordoVersao (histórico de aditivos/versões).
    /// </summary>
    public class AditivoRepositorio : IAditivoRepositorio
    {
        private readonly IDbConnectionFactory _factory;

        public AditivoRepositorio(IDbConnectionFactory factory)
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

        public async Task<IEnumerable<AcordoVersao>> ListarPorAcordoAsync(int tipoAcordoId)
        {
            const string sql = @"
        SELECT *
        FROM   dbo.AcordoVersao
        WHERE  TipoAcordoId = @tipoAcordoId
        ORDER  BY Versao;";

            using var conn = _factory.CreateConnection();
            return await conn.QueryAsync<AcordoVersao>(sql, new { tipoAcordoId });
        }

        public async Task<AcordoVersao?> ObterVersaoAtualAsync(int tipoAcordoId)
        {
            const string sql = @"
        SELECT TOP 1 *
        FROM   dbo.AcordoVersao
        WHERE  TipoAcordoId = @tipoAcordoId
        AND  VigenciaFim IS NULL
        ORDER  BY Versao DESC;";

            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<AcordoVersao>(sql, new { tipoAcordoId });
        }
        public async Task<(IEnumerable<AcordoVersao>, int)> 
        ListarPaginadoAsync(int acordoId, int pagina, int tamPag = 5)
        {
            const string sqlItens = @"
        SELECT * FROM dbo.AcordoVersao
        WHERE  TipoAcordoId = @acordoId
        ORDER  BY Versao DESC
        OFFSET (@pagina-1)*@tam ROWS FETCH NEXT @tam ROWS ONLY;";

            const string sqlCount = @"
        SELECT COUNT(*) FROM dbo.AcordoVersao
        WHERE TipoAcordoId = @acordoId;";

            using var conn = _factory.CreateConnection();
            var itens  = await conn.QueryAsync<AcordoVersao>(sqlItens,
                            new { acordoId, pagina, tam = tamPag });
            int total  = await conn.ExecuteScalarAsync<int>(sqlCount, new { acordoId });
            int totPag = (int)Math.Ceiling(total / (double)tamPag);
            return (itens, totPag);
        }
    }
}