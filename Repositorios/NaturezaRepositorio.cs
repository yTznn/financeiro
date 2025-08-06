using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Financeiro.Infraestrutura;
using Financeiro.Models;
using Financeiro.Models.ViewModels;

namespace Financeiro.Repositorios
{
    public class NaturezaRepositorio : INaturezaRepositorio
    {
        private readonly IDbConnectionFactory _factory;

        public NaturezaRepositorio(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task InserirAsync(NaturezaViewModel vm)
        {
            const string sql = @"
INSERT INTO dbo.Natureza (Nome, NaturezaMedica, Ativo)
VALUES (@Nome, @NaturezaMedica, @Ativo);";

            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(sql, vm);
        }

        public async Task AtualizarAsync(int id, NaturezaViewModel vm)
        {
            const string sql = @"
UPDATE dbo.Natureza
SET    Nome           = @Nome,
       NaturezaMedica = @NaturezaMedica,
       Ativo          = @Ativo
WHERE  Id = @Id;";

            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(sql, new
            {
                vm.Nome,
                vm.NaturezaMedica,
                vm.Ativo,
                Id = id
            });
        }

        public async Task<Natureza?> ObterPorIdAsync(int id)
        {
            const string sql = "SELECT * FROM dbo.Natureza WHERE Id = @id;";

            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<Natureza>(sql, new { id });
        }

        public async Task<IEnumerable<Natureza>> ListarAsync()
        {
            const string sql = "SELECT * FROM dbo.Natureza ORDER BY Nome;";

            using var conn = _factory.CreateConnection();
            return await conn.QueryAsync<Natureza>(sql);
        }
    }
}