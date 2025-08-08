using Dapper;
using Financeiro.Models;
using System.Data;
using System.Threading.Tasks;
using Financeiro.Infraestrutura;

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
            using var connection = _connectionFactory.CreateConnection();

            var sql = @"
                INSERT INTO Logs 
                    (UsuarioId, EntidadeId, Acao, Tabela, DataHora, ValoresAnteriores, ValoresNovos)
                VALUES 
                    (@UsuarioId, @EntidadeId, @Acao, @Tabela, @DataHora, @ValoresAnteriores, @ValoresNovos)";

            await connection.ExecuteAsync(sql, log);
        }
    }
}