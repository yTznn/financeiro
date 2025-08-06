using System.Data;
using System.Threading.Tasks;
using Dapper;
using Financeiro.Infraestrutura;
using Financeiro.Models;

namespace Financeiro.Repositorios
{
    public class ArquivoRepositorio : IArquivoRepositorio
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public ArquivoRepositorio(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<int> AdicionarAsync(Arquivo arquivo)
        {
            const string sql = @"
                INSERT INTO Arquivos
                (NomeOriginal, Extensao, Hash, Conteudo, Tamanho, ContentType, DataEnvio, Origem, ChaveReferencia)
                VALUES
                (@NomeOriginal, @Extensao, @Hash, @Conteudo, @Tamanho, @ContentType, @DataEnvio, @Origem, @ChaveReferencia);
                SELECT CAST(SCOPE_IDENTITY() AS INT);
            ";

            using var conn = _connectionFactory.CreateConnection();
            return await conn.ExecuteScalarAsync<int>(sql, arquivo);
        }

        public async Task<Arquivo?> ObterPorIdAsync(int id)
        {
            const string sql = @"
                SELECT *
                FROM Arquivos
                WHERE Id = @Id
            ";

            using var conn = _connectionFactory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<Arquivo>(sql, new { Id = id });
        }
    }
}