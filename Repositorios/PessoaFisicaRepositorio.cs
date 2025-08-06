using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Financeiro.Infraestrutura;
using Financeiro.Models;
using Financeiro.Models.ViewModels;

namespace Financeiro.Repositorios
{
    public class PessoaFisicaRepositorio : IPessoaFisicaRepositorio
    {
        private readonly IDbConnectionFactory _factory;
        public PessoaFisicaRepositorio(IDbConnectionFactory factory) => _factory = factory;

        public async Task InserirAsync(PessoaFisicaViewModel vm)
        {
            const string sql = @"
INSERT INTO PessoaFisica
      (Nome, Sobrenome, Cpf, DataNascimento, Email, Telefone, SituacaoAtiva)
VALUES(@Nome, @Sobrenome, @Cpf, @DataNascimento, @Email, @Telefone, @SituacaoAtiva);";

            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(sql, vm);
        }

        public async Task<IEnumerable<PessoaFisica>> ListarAsync()
        {
            const string sql = "SELECT * FROM PessoaFisica ORDER BY Id DESC;";
            using var conn = _factory.CreateConnection();
            return await conn.QueryAsync<PessoaFisica>(sql);
        }

        public async Task<PessoaFisica?> ObterPorIdAsync(int id)
        {
            const string sql = "SELECT * FROM PessoaFisica WHERE Id = @Id;";
            using var conn = _factory.CreateConnection();
            return await conn.QuerySingleOrDefaultAsync<PessoaFisica>(sql, new { Id = id });
        }

        public async Task AtualizarAsync(int id, PessoaFisicaViewModel vm)
        {
            const string sql = @"
UPDATE PessoaFisica SET
      Nome            = @Nome,
      Sobrenome       = @Sobrenome,
      Cpf             = @Cpf,
      DataNascimento  = @DataNascimento,
      Email           = @Email,
      Telefone        = @Telefone,
      SituacaoAtiva   = @SituacaoAtiva
WHERE Id = @Id;";

            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(sql, new
            {
                Id = id,
                vm.Nome,
                vm.Sobrenome,
                vm.Cpf,
                vm.DataNascimento,
                vm.Email,
                vm.Telefone,
                vm.SituacaoAtiva
            });
        }
    }
}