// Repositorios/PessoaJuridicaRepositorio.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Financeiro.Infraestrutura;
using Financeiro.Models;
using Financeiro.Models.ViewModels;

namespace Financeiro.Repositorios
{
    public class PessoaJuridicaRepositorio : IPessoaJuridicaRepositorio
    {
        private readonly IDbConnectionFactory _factory;
        public PessoaJuridicaRepositorio(IDbConnectionFactory factory) => _factory = factory;

        public async Task InserirAsync(PessoaJuridicaViewModel vm)
        {
            const string sql = @"
INSERT INTO PessoaJuridica
      (RazaoSocial, NomeFantasia, NumeroInscricao, Email, Telefone, SituacaoAtiva)
VALUES(@RazaoSocial, @NomeFantasia, @NumeroInscricao, @Email, @Telefone, @SituacaoAtiva);";

            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(sql, vm);
        }

        public async Task<IEnumerable<PessoaJuridica>> ListarAsync()
        {
            const string sql = "SELECT * FROM PessoaJuridica ORDER BY Id DESC;";
            using var conn = _factory.CreateConnection();
            return await conn.QueryAsync<PessoaJuridica>(sql);
        }

        /* ---------- NOVOS MÉTODOS ---------- */

        public async Task<PessoaJuridica?> ObterPorIdAsync(int id)
        {
            const string sql = "SELECT * FROM PessoaJuridica WHERE Id = @Id;";
            using var conn = _factory.CreateConnection();
            return await conn.QuerySingleOrDefaultAsync<PessoaJuridica>(sql, new { Id = id });
        }

        public async Task AtualizarAsync(int id, PessoaJuridicaViewModel vm)
        {
            const string sql = @"
UPDATE PessoaJuridica SET
      RazaoSocial     = @RazaoSocial,
      NomeFantasia    = @NomeFantasia,
      NumeroInscricao = @NumeroInscricao,
      Email           = @Email,
      Telefone        = @Telefone,
      SituacaoAtiva   = @SituacaoAtiva
WHERE Id = @Id;";

            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(sql, new
            {
                Id = id,
                vm.RazaoSocial,
                vm.NomeFantasia,
                vm.NumeroInscricao,
                vm.Email,
                vm.Telefone,
                vm.SituacaoAtiva
            });
        }
    }
}