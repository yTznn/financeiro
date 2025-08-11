using Dapper;
using Financeiro.Infraestrutura;
using Financeiro.Models;
using System.Threading.Tasks;

namespace Financeiro.Repositorios
{
    public class LogRepositorio : ILogRepositorio
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public LogRepositorio(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task RegistrarAsync(Log log)
        {
            const string sql = @"
                INSERT INTO LogAcao
                    (UsuarioId, EntidadeId, Acao, Tabela, DataHora,
                     ValoresAnteriores, ValoresNovos, RegistroId)
                VALUES
                    (@UsuarioId, @EntidadeId, @Acao, @Tabela, @DataHora,
                     @ValoresAnteriores, @ValoresNovos, @RegistroId);";

            using var connection = _connectionFactory.CreateConnection();
            await connection.ExecuteAsync(sql, log);   // passa o objeto log direto
        }
    }
}