using System.Threading.Tasks;
using Dapper;
using Financeiro.Infraestrutura;
using Financeiro.Models;
using Financeiro.Models.ViewModels;

namespace Financeiro.Repositorios
{
    public class ContaBancariaRepositorio : IContaBancariaRepositorio
    {
        private readonly IDbConnectionFactory _factory;

        public ContaBancariaRepositorio(IDbConnectionFactory factory) => _factory = factory;

        /* -------- Consultas -------- */

        public async Task<ContaBancaria?> ObterPorPessoaJuridicaAsync(int pessoaJuridicaId)
        {
            const string sql = @"
SELECT cb.*
  FROM ContaBancaria cb
  INNER JOIN PessoaConta pc ON pc.ContaBancariaId = cb.Id
 WHERE pc.PessoaJuridicaId = @pessoaJuridicaId;";

            using var conn = _factory.CreateConnection();
            return await conn.QuerySingleOrDefaultAsync<ContaBancaria>(sql, new { pessoaJuridicaId });
        }

        public async Task<ContaBancaria?> ObterPorPessoaFisicaAsync(int pessoaFisicaId)
        {
            const string sql = @"
SELECT cb.*
  FROM ContaBancaria cb
  INNER JOIN PessoaConta pc ON pc.ContaBancariaId = cb.Id
 WHERE pc.PessoaFisicaId = @pessoaFisicaId;";

            using var conn = _factory.CreateConnection();
            return await conn.QuerySingleOrDefaultAsync<ContaBancaria>(sql, new { pessoaFisicaId });
        }

        /* -------- Gravação -------- */

        public async Task InserirAsync(ContaBancariaViewModel vm)
        {
            // Garante que apenas um dos IDs esteja preenchido
            if ((vm.PessoaJuridicaId is null && vm.PessoaFisicaId is null) ||
                (vm.PessoaJuridicaId is not null && vm.PessoaFisicaId is not null))
                throw new InvalidOperationException("Informe somente PessoaJuridicaId ou PessoaFisicaId.");

            const string insertConta = @"
INSERT INTO ContaBancaria (Banco, Agencia, Conta, ChavePix)
VALUES (@Banco, @Agencia, @Conta, @ChavePix);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            using var conn = _factory.CreateConnection();
            var contaId = await conn.QuerySingleAsync<int>(insertConta, vm);

            const string link = @"
INSERT INTO PessoaConta (PessoaJuridicaId, PessoaFisicaId, ContaBancariaId)
VALUES (@PessoaJuridicaId, @PessoaFisicaId, @ContaId);";

            await conn.ExecuteAsync(link, new
            {
                vm.PessoaJuridicaId,
                vm.PessoaFisicaId,
                ContaId = contaId
            });
        }

        public async Task AtualizarAsync(int id, ContaBancariaViewModel vm)
        {
            const string sql = @"
UPDATE ContaBancaria SET
    Banco    = @Banco,
    Agencia  = @Agencia,
    Conta    = @Conta,
    ChavePix = @ChavePix
WHERE Id = @Id;";

            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(sql, new
            {
                Id = id,
                vm.Banco,
                vm.Agencia,
                vm.Conta,
                vm.ChavePix
            });
        }
    }
}