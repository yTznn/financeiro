using Dapper;
using Financeiro.Models;
using Financeiro.Repositorios;
using Financeiro.Infraestrutura;

namespace Financeiro.Repositorios
{
    public class EntidadeRepositorio : IEntidadeRepositorio
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public EntidadeRepositorio(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<int> AddAsync(Entidade entidade)
        {
            const string sql = @"
INSERT INTO Entidade
    (Nome, Sigla, Cnpj, ContaBancariaId, EnderecoId, Ativo, VinculaUsuario, Observacao)
VALUES
    (@Nome, @Sigla, @Cnpj, @ContaBancariaId, @EnderecoId, @Ativo, @VinculaUsuario, @Observacao);

SELECT CAST(SCOPE_IDENTITY() AS INT);";

            using var conn = _connectionFactory.CreateConnection();
            return await conn.QuerySingleAsync<int>(sql, entidade);
        }

        public async Task UpdateAsync(Entidade entidade)
        {
            const string sql = @"
UPDATE Entidade
SET Nome            = @Nome,
    Sigla           = @Sigla,
    Cnpj            = @Cnpj,
    ContaBancariaId = @ContaBancariaId,
    EnderecoId      = @EnderecoId,
    Ativo           = @Ativo,
    VinculaUsuario  = @VinculaUsuario,
    Observacao      = @Observacao
WHERE Id = @Id;";

            using var conn = _connectionFactory.CreateConnection();
            await conn.ExecuteAsync(sql, entidade);
        }

        public async Task DeleteAsync(int id)
        {
            const string sql = "DELETE FROM Entidade WHERE Id = @Id;";
            using var conn = _connectionFactory.CreateConnection();
            await conn.ExecuteAsync(sql, new { Id = id });
        }

        public async Task<Entidade?> GetByIdAsync(int id)
        {
            const string sql = "SELECT * FROM Entidade WHERE Id = @Id;";
            using var conn = _connectionFactory.CreateConnection();
            return await conn.QuerySingleOrDefaultAsync<Entidade>(sql, new { Id = id });
        }

        public async Task<Entidade?> GetByCnpjAsync(string cnpj)
        {
            const string sql = "SELECT * FROM Entidade WHERE Cnpj = @Cnpj;";
            using var conn = _connectionFactory.CreateConnection();
            return await conn.QuerySingleOrDefaultAsync<Entidade>(sql, new { Cnpj = cnpj });
        }

        public async Task<IEnumerable<Entidade>> ListAsync()
        {
            const string sql = "SELECT * FROM Entidade ORDER BY Nome;";
            using var conn = _connectionFactory.CreateConnection();
            return await conn.QueryAsync<Entidade>(sql);
        }
    }
}