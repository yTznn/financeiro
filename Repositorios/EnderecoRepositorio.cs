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
            conn.Open();
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

        public async Task DefinirPrincipalPessoaJuridicaAsync(int pessoaJuridicaId, int enderecoId)
        {
            using var conn = _factory.CreateConnection();
            conn.Open();
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

        /* ===================== NOVO (múltiplos endereços PF) ===================== */

        public async Task<IEnumerable<Endereco>> ListarPorPessoaFisicaAsync(int pessoaFisicaId)
        {
            const string sql = @"
SELECT e.*
  FROM Endereco e
  INNER JOIN PessoaEndereco pe ON pe.EnderecoId = e.Id
 WHERE pe.PessoaFisicaId = @pessoaFisicaId
 ORDER BY pe.Principal DESC, e.Logradouro;";

            using var conn = _factory.CreateConnection();
            return await conn.QueryAsync<Endereco>(sql, new { pessoaFisicaId });
        }

        public async Task<Endereco?> ObterPrincipalPorPessoaFisicaAsync(int pessoaFisicaId)
        {
            const string sql = @"
SELECT TOP (1) e.*
  FROM Endereco e
  INNER JOIN PessoaEndereco pe ON pe.EnderecoId = e.Id
 WHERE pe.PessoaFisicaId = @pessoaFisicaId
   AND pe.Principal = 1;";

            using var conn = _factory.CreateConnection();
            return await conn.QuerySingleOrDefaultAsync<Endereco>(sql, new { pessoaFisicaId });
        }

        public async Task DefinirPrincipalPessoaFisicaAsync(int pessoaFisicaId, int enderecoId)
        {
            using var conn = _factory.CreateConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();

            const string upsert = @"
IF NOT EXISTS (
    SELECT 1 FROM PessoaEndereco
     WHERE PessoaFisicaId = @pessoaFisicaId AND EnderecoId = @enderecoId
)
BEGIN
    INSERT INTO PessoaEndereco (PessoaFisicaId, EnderecoId, Principal)
    VALUES (@pessoaFisicaId, @enderecoId, 0);
END;";

            const string reset = @"UPDATE PessoaEndereco SET Principal = 0 WHERE PessoaFisicaId = @pessoaFisicaId;";
            const string set   = @"UPDATE PessoaEndereco SET Principal = 1 WHERE PessoaFisicaId = @pessoaFisicaId AND EnderecoId = @enderecoId;";

            await conn.ExecuteAsync(upsert, new { pessoaFisicaId, enderecoId }, tx);
            await conn.ExecuteAsync(reset , new { pessoaFisicaId }, tx);
            await conn.ExecuteAsync(set   , new { pessoaFisicaId, enderecoId }, tx);

            tx.Commit();
        }

        public async Task VincularPessoaFisicaAsync(int pessoaFisicaId, int enderecoId, bool ativo = true)
        {
            const string sql = @"
IF NOT EXISTS (
    SELECT 1 FROM PessoaEndereco
     WHERE PessoaFisicaId = @pessoaFisicaId AND EnderecoId = @enderecoId
)
BEGIN
    INSERT INTO PessoaEndereco (PessoaFisicaId, EnderecoId, Principal)
    VALUES (@pessoaFisicaId, @enderecoId, 0);
END;";

            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(sql, new { pessoaFisicaId, enderecoId });
        }

        public async Task<bool> PossuiPrincipalPessoaFisicaAsync(int pessoaFisicaId)
        {
            const string sql = @"
SELECT CASE WHEN EXISTS (
    SELECT 1 FROM PessoaEndereco 
     WHERE PessoaFisicaId = @pessoaFisicaId AND Principal = 1
) THEN 1 ELSE 0 END;";

            using var conn = _factory.CreateConnection();
            return await conn.ExecuteScalarAsync<bool>(sql, new { pessoaFisicaId });
        }

        /* ===================== UTILIDADE (reuso geral) ===================== */

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

        /* ===================== NOVO (editar/excluir por Id) ===================== */

        // NOVO
        public async Task<Endereco?> ObterPorIdAsync(int enderecoId)
        {
            const string sql = @"SELECT * FROM Endereco WHERE Id = @enderecoId;";
            using var conn = _factory.CreateConnection();
            return await conn.QuerySingleOrDefaultAsync<Endereco>(sql, new { enderecoId });
        }

        // NOVO — Exclusão PJ com reassinamento de principal se necessário
        public async Task ExcluirEnderecoPessoaJuridicaAsync(int pessoaJuridicaId, int enderecoId)
        {
            using var conn = _factory.CreateConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();

            // identifica se o vínculo a remover é principal
            const string selPrincipal = @"
SELECT Principal FROM PessoaEndereco
 WHERE PessoaJuridicaId = @pessoaJuridicaId AND EnderecoId = @enderecoId;";
            var eraPrincipal = await conn.ExecuteScalarAsync<bool?>(selPrincipal, new { pessoaJuridicaId, enderecoId }, tx) ?? false;

            // remove vínculo
            const string delVinculo = @"
DELETE FROM PessoaEndereco
 WHERE PessoaJuridicaId = @pessoaJuridicaId AND EnderecoId = @enderecoId;";
            await conn.ExecuteAsync(delVinculo, new { pessoaJuridicaId, enderecoId }, tx);

            // se o endereço ficou órfão (sem vínculos), apaga o endereço
            const string existeVinculo = @"SELECT 1 FROM PessoaEndereco WHERE EnderecoId = @enderecoId;";
            var aindaVinculado = await conn.QueryFirstOrDefaultAsync<int?>(existeVinculo, new { enderecoId }, tx).ConfigureAwait(false);
            if (aindaVinculado is null)
            {
                const string delEndereco = @"DELETE FROM Endereco WHERE Id = @enderecoId;";
                await conn.ExecuteAsync(delEndereco, new { enderecoId }, tx);
            }

            // se removemos o principal, escolhe um novo (se houver)
            if (eraPrincipal)
            {
                const string reset = @"UPDATE PessoaEndereco SET Principal = 0 WHERE PessoaJuridicaId = @pessoaJuridicaId;";
                await conn.ExecuteAsync(reset, new { pessoaJuridicaId }, tx);

                const string pickNovo = @"
SELECT TOP (1) EnderecoId
  FROM PessoaEndereco
 WHERE PessoaJuridicaId = @pessoaJuridicaId
 ORDER BY EnderecoId;"; // simples e determinístico; ajuste se quiser outra ordenação

                var novoId = await conn.QuerySingleOrDefaultAsync<int?>(pickNovo, new { pessoaJuridicaId }, tx);
                if (novoId.HasValue)
                {
                    const string setNovo = @"
UPDATE PessoaEndereco SET Principal = 1
 WHERE PessoaJuridicaId = @pessoaJuridicaId AND EnderecoId = @novoId;";
                    await conn.ExecuteAsync(setNovo, new { pessoaJuridicaId, novoId }, tx);
                }
            }

            tx.Commit();
        }

        // NOVO — Exclusão PF com reassinamento de principal se necessário
        public async Task ExcluirEnderecoPessoaFisicaAsync(int pessoaFisicaId, int enderecoId)
        {
            using var conn = _factory.CreateConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();

            const string selPrincipal = @"
SELECT Principal FROM PessoaEndereco
 WHERE PessoaFisicaId = @pessoaFisicaId AND EnderecoId = @enderecoId;";
            var eraPrincipal = await conn.ExecuteScalarAsync<bool?>(selPrincipal, new { pessoaFisicaId, enderecoId }, tx) ?? false;

            const string delVinculo = @"
DELETE FROM PessoaEndereco
 WHERE PessoaFisicaId = @pessoaFisicaId AND EnderecoId = @enderecoId;";
            await conn.ExecuteAsync(delVinculo, new { pessoaFisicaId, enderecoId }, tx);

            const string existeVinculo = @"SELECT 1 FROM PessoaEndereco WHERE EnderecoId = @enderecoId;";
            var aindaVinculado = await conn.QueryFirstOrDefaultAsync<int?>(existeVinculo, new { enderecoId }, tx).ConfigureAwait(false);
            if (aindaVinculado is null)
            {
                const string delEndereco = @"DELETE FROM Endereco WHERE Id = @enderecoId;";
                await conn.ExecuteAsync(delEndereco, new { enderecoId }, tx);
            }

            if (eraPrincipal)
            {
                const string reset = @"UPDATE PessoaEndereco SET Principal = 0 WHERE PessoaFisicaId = @pessoaFisicaId;";
                await conn.ExecuteAsync(reset, new { pessoaFisicaId }, tx);

                const string pickNovo = @"
SELECT TOP (1) EnderecoId
  FROM PessoaEndereco
 WHERE PessoaFisicaId = @pessoaFisicaId
 ORDER BY EnderecoId;";

                var novoId = await conn.QuerySingleOrDefaultAsync<int?>(pickNovo, new { pessoaFisicaId }, tx);
                if (novoId.HasValue)
                {
                    const string setNovo = @"
UPDATE PessoaEndereco SET Principal = 1
 WHERE PessoaFisicaId = @pessoaFisicaId AND EnderecoId = @novoId;";
                    await conn.ExecuteAsync(setNovo, new { pessoaFisicaId, novoId }, tx);
                }
            }

            tx.Commit();
        }
    }
}