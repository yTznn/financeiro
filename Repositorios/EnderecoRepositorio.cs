using System.Collections.Generic;
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

        /* ===================== LEGADO (único endereço PJ) ===================== */

        /// <summary>
        /// Retorna um único endereço vinculado à pessoa jurídica (legado).
        /// Prefira usar ListarPorPessoaJuridicaAsync no novo fluxo.
        /// </summary>
        public async Task<Endereco?> ObterPorPessoaAsync(int pessoaJuridicaId)
        {
            const string sql = @"
SELECT TOP (1) e.*
  FROM Endereco e
  INNER JOIN PessoaEndereco pe ON pe.EnderecoId = e.Id
 WHERE pe.PessoaJuridicaId = @pessoaJuridicaId;";

            using var conn = _factory.CreateConnection();
            return await conn.QuerySingleOrDefaultAsync<Endereco>(sql, new { pessoaJuridicaId });
        }

        /// <summary>
        /// Insere o endereço (Endereco) e cria o vínculo (PessoaEndereco).
        /// Se a pessoa ainda não tiver um principal, define este como principal.
        /// </summary>
        public async Task InserirAsync(EnderecoViewModel vm)
        {
            const string insertEndereco = @"
INSERT INTO Endereco
      (Logradouro, Numero, Complemento, Cep, Bairro, Municipio, Uf)
VALUES(@Logradouro, @Numero, @Complemento, @Cep, @Bairro, @Municipio, @Uf);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            const string insertVinculo = @"
INSERT INTO PessoaEndereco (PessoaJuridicaId, EnderecoId, Principal)
VALUES (@PessoaJuridicaId, @EnderecoId, 0);";

            const string setPrincipalSeNaoExiste = @"
IF NOT EXISTS (
    SELECT 1 FROM PessoaEndereco 
     WHERE PessoaJuridicaId = @PessoaJuridicaId AND Principal = 1
)
BEGIN
    UPDATE PessoaEndereco
       SET Principal = 1
     WHERE PessoaJuridicaId = @PessoaJuridicaId
       AND EnderecoId       = @EnderecoId;
END;";

            using var conn = _factory.CreateConnection();
            conn.Open(); // <— trocado de OpenAsync para Open
            using var tx = conn.BeginTransaction();

            var enderecoId = await conn.QuerySingleAsync<int>(insertEndereco, vm, tx);

            await conn.ExecuteAsync(insertVinculo, new
            {
                vm.PessoaJuridicaId,
                EnderecoId = enderecoId
            }, tx);

            await conn.ExecuteAsync(setPrincipalSeNaoExiste, new
            {
                vm.PessoaJuridicaId,
                EnderecoId = enderecoId
            }, tx);

            tx.Commit();
        }

        /// <summary>
        /// Atualiza os campos do endereço na tabela Endereco.
        /// </summary>
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
                Id = id,
                vm.Logradouro,
                vm.Numero,
                vm.Complemento,
                vm.Cep,
                vm.Bairro,
                vm.Municipio,
                vm.Uf
            });
        }

        /* ===================== NOVO (múltiplos endereços PJ) ===================== */

        /// <summary>
        /// Lista todos os endereços de uma Pessoa Jurídica (principal primeiro).
        /// </summary>
        public async Task<IEnumerable<Endereco>> ListarPorPessoaJuridicaAsync(int pessoaJuridicaId)
        {
            const string sql = @"
SELECT e.*
  FROM Endereco e
  INNER JOIN PessoaEndereco pe ON pe.EnderecoId = e.Id
 WHERE pe.PessoaJuridicaId = @pessoaJuridicaId
 ORDER BY pe.Principal DESC, e.Logradouro;";

            using var conn = _factory.CreateConnection();
            return await conn.QueryAsync<Endereco>(sql, new { pessoaJuridicaId });
        }

        /// <summary>
        /// Retorna o endereço principal de uma Pessoa Jurídica (ou null).
        /// </summary>
        public async Task<Endereco?> ObterPrincipalPorPessoaJuridicaAsync(int pessoaJuridicaId)
        {
            const string sql = @"
SELECT TOP (1) e.*
  FROM Endereco e
  INNER JOIN PessoaEndereco pe ON pe.EnderecoId = e.Id
 WHERE pe.PessoaJuridicaId = @pessoaJuridicaId
   AND pe.Principal = 1;";

            using var conn = _factory.CreateConnection();
            return await conn.QuerySingleOrDefaultAsync<Endereco>(sql, new { pessoaJuridicaId });
        }

        /// <summary>
        /// Define o endereço como principal para a Pessoa Jurídica (operação atômica).
        /// </summary>
        public async Task DefinirPrincipalPessoaJuridicaAsync(int pessoaJuridicaId, int enderecoId)
        {
            using var conn = _factory.CreateConnection();
            conn.Open(); // <— trocado de OpenAsync para Open
            using var tx = conn.BeginTransaction();

            const string upsertVinculo = @"
IF NOT EXISTS (
    SELECT 1 FROM PessoaEndereco 
     WHERE PessoaJuridicaId = @pessoaJuridicaId AND EnderecoId = @enderecoId
)
BEGIN
    INSERT INTO PessoaEndereco (PessoaJuridicaId, EnderecoId, Principal)
    VALUES (@pessoaJuridicaId, @enderecoId, 0);
END;";

            const string resetPrincipais = @"
UPDATE PessoaEndereco 
   SET Principal = 0 
 WHERE PessoaJuridicaId = @pessoaJuridicaId;";

            const string setPrincipal = @"
UPDATE PessoaEndereco 
   SET Principal = 1
 WHERE PessoaJuridicaId = @pessoaJuridicaId
   AND EnderecoId       = @enderecoId;";

            await conn.ExecuteAsync(upsertVinculo, new { pessoaJuridicaId, enderecoId }, tx);
            await conn.ExecuteAsync(resetPrincipais, new { pessoaJuridicaId }, tx);
            await conn.ExecuteAsync(setPrincipal, new { pessoaJuridicaId, enderecoId }, tx);

            tx.Commit();
        }

        /// <summary>
        /// Cria o vínculo em PessoaEndereco (Principal = 0 por padrão).
        /// Ignora o parâmetro 'ativo' (a tabela não possui esta coluna).
        /// </summary>
        public async Task VincularPessoaJuridicaAsync(int pessoaJuridicaId, int enderecoId, bool ativo = true)
        {
            const string sql = @"
IF NOT EXISTS (
    SELECT 1 FROM PessoaEndereco
     WHERE PessoaJuridicaId = @pessoaJuridicaId AND EnderecoId = @enderecoId
)
BEGIN
    INSERT INTO PessoaEndereco (PessoaJuridicaId, EnderecoId, Principal)
    VALUES (@pessoaJuridicaId, @enderecoId, 0);
END;";

            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(sql, new { pessoaJuridicaId, enderecoId });
        }

        /// <summary>
        /// Indica se a Pessoa Jurídica já possui um endereço principal.
        /// </summary>
        public async Task<bool> PossuiPrincipalPessoaJuridicaAsync(int pessoaJuridicaId)
        {
            const string sql = @"
SELECT CASE WHEN EXISTS (
    SELECT 1 FROM PessoaEndereco 
     WHERE PessoaJuridicaId = @pessoaJuridicaId AND Principal = 1
) THEN 1 ELSE 0 END;";

            using var conn = _factory.CreateConnection();
            return await conn.ExecuteScalarAsync<bool>(sql, new { pessoaJuridicaId });
        }

        /* ===================== UTILIDADE (reuso geral) ===================== */

        /// <summary>
        /// Insere em Endereco e retorna o Id gerado (sem criar vínculos).
        /// </summary>
        public async Task<int> InserirRetornandoIdAsync(Endereco endereco)
        {
            const string sql = @"
INSERT INTO Endereco
      (Logradouro, Numero, Complemento, Cep, Bairro, Municipio, Uf)
VALUES(@Logradouro, @Numero, @Complemento, @Cep, @Bairro, @Municipio, @Uf);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            using var conn = _factory.CreateConnection();
            return await conn.QuerySingleAsync<int>(sql, endereco);
        }
    }
}