using System.Threading.Tasks;
using Dapper;
using Financeiro.Infraestrutura;
using Microsoft.Data.SqlClient;

namespace Financeiro.Servicos
{
    public class JustificativaService : IJustificativaService
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public JustificativaService(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task RegistrarAsync(int usuarioId, int entidadeId, string tabela, string acao, int registroId, string texto)
        {
            using var connection = _connectionFactory.CreateConnection();

            var sql = @"
                INSERT INTO Justificativa (UsuarioId, EntidadeId, Tabela, Acao, RegistroId, Texto)
                VALUES (@UsuarioId, @EntidadeId, @Tabela, @Acao, @RegistroId, @Texto)";

            await connection.ExecuteAsync(sql, new
            {
                UsuarioId = usuarioId,
                EntidadeId = entidadeId,
                Tabela = tabela,
                Acao = acao,
                RegistroId = registroId,
                Texto = texto
            });
        }
    }
}