using System.Threading.Tasks;
using Dapper;
using Financeiro.Infraestrutura;
using Financeiro.Models;
using Financeiro.Models.ViewModels;

namespace Financeiro.Repositorios
{
    public class EnderecoRepositorio : IEnderecoRepositorio
    {
        private readonly IDbConnectionFactory _factory;
        public EnderecoRepositorio(IDbConnectionFactory factory) => _factory = factory;

        public async Task<Endereco?> ObterPorPessoaAsync(int pessoaJuridicaId)
        {
            const string sql = @"
SELECT e.*
  FROM Endereco e
  INNER JOIN PessoaEndereco pe ON pe.EnderecoId = e.Id
 WHERE pe.PessoaJuridicaId = @pessoaJuridicaId;";

            using var conn = _factory.CreateConnection();
            return await conn.QuerySingleOrDefaultAsync<Endereco>(sql, new { pessoaJuridicaId });
        }

        public async Task InserirAsync(EnderecoViewModel vm)
        {
            const string insertEndereco = @"
INSERT INTO Endereco
      (Logradouro, Numero, Complemento, Cep, Bairro, Municipio, Uf)
VALUES(@Logradouro, @Numero, @Complemento, @Cep, @Bairro, @Municipio, @Uf);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            using var conn = _factory.CreateConnection();
            var enderecoId = await conn.QuerySingleAsync<int>(insertEndereco, vm);

            const string link = @"
INSERT INTO PessoaEndereco (PessoaJuridicaId, EnderecoId)
VALUES (@PessoaJuridicaId, @EnderecoId);";

            await conn.ExecuteAsync(link, new { vm.PessoaJuridicaId, EnderecoId = enderecoId });
        }

        public async Task AtualizarAsync(int id, EnderecoViewModel vm)
        {
            const string sql = @"
UPDATE Endereco SET
    Logradouro  = @Logradouro,
    Numero      = @Numero,
    Complemento = @Complemento,
    Cep         = @Cep,
    Bairro      = @Bairro,
    Municipio   = @Municipio,
    Uf          = @Uf
WHERE Id = @Id;";

            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(sql, new
            {
                Id            = id,
                vm.Logradouro,
                vm.Numero,
                vm.Complemento,
                vm.Cep,
                vm.Bairro,
                vm.Municipio,
                vm.Uf
            });
        }
    }
}