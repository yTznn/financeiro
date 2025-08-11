using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Financeiro.Infraestrutura;
using Financeiro.Models;

namespace Financeiro.Repositorios
{
    /// <summary>
    /// Repositório para vínculo de endereços da Entidade (EntidadeEndereco).
    /// Suporta múltiplos endereços e garante exatamente um principal por Entidade.
    /// </summary>
    public class EntidadeEnderecoRepositorio : IEntidadeEnderecoRepositorio
    {
        private readonly IDbConnectionFactory _factory;

        public EntidadeEnderecoRepositorio(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        /// <summary>Lista endereços da Entidade (ativos), com principal primeiro.</summary>
        public async Task<IEnumerable<Endereco>> ListarPorEntidadeAsync(int entidadeId)
        {
            const string sql = @"
SELECT e.*
  FROM Endereco e
  INNER JOIN EntidadeEndereco ee ON ee.EnderecoId = e.Id
 WHERE ee.EntidadeId = @entidadeId
   AND ee.Ativo = 1
 ORDER BY ee.Principal DESC, e.Logradouro;";

            using var conn = _factory.CreateConnection();
            return await conn.QueryAsync<Endereco>(sql, new { entidadeId });
        }

        /// <summary>Obtém o endereço principal da Entidade (se houver).</summary>
        public async Task<Endereco?> ObterPrincipalPorEntidadeAsync(int entidadeId)
        {
            const string sql = @"
SELECT TOP (1) e.*
  FROM Endereco e
  INNER JOIN EntidadeEndereco ee ON ee.EnderecoId = e.Id
 WHERE ee.EntidadeId = @entidadeId
   AND ee.Ativo = 1
   AND ee.Principal = 1;";

            using var conn = _factory.CreateConnection();
            return await conn.QuerySingleOrDefaultAsync<Endereco>(sql, new { entidadeId });
        }

        /// <summary>
        /// Define um endereço como principal para a Entidade (troca atômica)
        /// e sincroniza ponteiro em <c>Entidade.EnderecoId</c>.
        /// </summary>
        public async Task DefinirPrincipalEntidadeAsync(int entidadeId, int enderecoId)
        {
            using var conn = _factory.CreateConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();

            // 1) Garante vínculo e ativa se necessário
            const string upsertVinculo = @"
IF NOT EXISTS (
    SELECT 1 FROM EntidadeEndereco 
     WHERE EntidadeId = @entidadeId AND EnderecoId = @enderecoId
)
BEGIN
    INSERT INTO EntidadeEndereco (EntidadeId, EnderecoId, Principal, Ativo)
    VALUES (@entidadeId, @enderecoId, 0, 1);
END
ELSE
BEGIN
    UPDATE EntidadeEndereco
       SET Ativo = 1
     WHERE EntidadeId = @entidadeId AND EnderecoId = @enderecoId;
END;";
            await conn.ExecuteAsync(upsertVinculo, new { entidadeId, enderecoId }, tx);

            // 2) Zera principais atuais
            const string resetPrincipais = @"
UPDATE EntidadeEndereco 
   SET Principal = 0
 WHERE EntidadeId = @entidadeId;";
            await conn.ExecuteAsync(resetPrincipais, new { entidadeId }, tx);

            // 3) Define o solicitado como principal
            const string setPrincipal = @"
UPDATE EntidadeEndereco
   SET Principal = 1
 WHERE EntidadeId = @entidadeId
   AND EnderecoId = @enderecoId;";
            await conn.ExecuteAsync(setPrincipal, new { entidadeId, enderecoId }, tx);

            // 4) Sincroniza ponteiro em Entidade.EnderecoId
            const string atualizaEntidade = @"
UPDATE Entidade 
   SET EnderecoId = @enderecoId
 WHERE Id = @entidadeId;";
            await conn.ExecuteAsync(atualizaEntidade, new { entidadeId, enderecoId }, tx);

            tx.Commit();
        }

        /// <summary>
        /// Cria (se não existir) ou reativa o vínculo Entidade↔Endereço com Principal = 0.
        /// Não mexe no principal atual.
        /// </summary>
        public async Task VincularAsync(int entidadeId, int enderecoId, bool ativo = true)
        {
            const string sql = @"
IF NOT EXISTS (
    SELECT 1 FROM EntidadeEndereco 
     WHERE EntidadeId = @entidadeId AND EnderecoId = @enderecoId
)
BEGIN
    INSERT INTO EntidadeEndereco (EntidadeId, EnderecoId, Principal, Ativo)
    VALUES (@entidadeId, @enderecoId, 0, @ativo);
END
ELSE
BEGIN
    UPDATE EntidadeEndereco
       SET Ativo = @ativo
     WHERE EntidadeId = @entidadeId AND EnderecoId = @enderecoId;
END;";

            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(sql, new { entidadeId, enderecoId, ativo = ativo ? 1 : 0 });
        }

        /// <summary>Indica se já existe endereço principal para a Entidade.</summary>
        public async Task<bool> PossuiPrincipalAsync(int entidadeId)
        {
            const string sql = @"
SELECT CASE WHEN EXISTS (
    SELECT 1 FROM EntidadeEndereco 
     WHERE EntidadeId = @entidadeId AND Ativo = 1 AND Principal = 1
) THEN 1 ELSE 0 END;";

            using var conn = _factory.CreateConnection();
            return await conn.ExecuteScalarAsync<bool>(sql, new { entidadeId });
        }

        /// <summary>
        /// Exclui o vínculo e, se o endereço ficar sem vínculos, apaga-o da tabela Endereco.
        /// Se era principal, escolhe um novo principal (se houver) ou zera Entidade.EnderecoId.
        /// </summary>
        public async Task<bool> ExcluirAsync(int entidadeId, int enderecoId)
        {
            using var conn = _factory.CreateConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();

            // 1) Era principal?
            const string sqlPrincipal = @"
SELECT TOP (1) Principal
  FROM EntidadeEndereco
 WHERE EntidadeId = @entidadeId AND EnderecoId = @enderecoId;";
            var eraPrincipal = await conn.ExecuteScalarAsync<int?>(sqlPrincipal, new { entidadeId, enderecoId }, tx) == 1;

            // 2) Apaga vínculo
            const string delVinc = @"
DELETE FROM EntidadeEndereco
 WHERE EntidadeId = @entidadeId AND EnderecoId = @enderecoId;";
            await conn.ExecuteAsync(delVinc, new { entidadeId, enderecoId }, tx);

            // 3) Se era principal: escolher um novo ou zerar ponteiro
            if (eraPrincipal)
            {
                // pega algum outro endereço (qualquer um)
                const string pickOutro = @"
SELECT TOP (1) EnderecoId
  FROM EntidadeEndereco
 WHERE EntidadeId = @entidadeId
 ORDER BY Principal DESC, EnderecoId ASC;";
                var novoPrincipalId = await conn.ExecuteScalarAsync<int?>(pickOutro, new { entidadeId }, tx);

                if (novoPrincipalId.HasValue)
                {
                    const string zerar = @"
UPDATE EntidadeEndereco SET Principal = 0 
 WHERE EntidadeId = @entidadeId;";
                    await conn.ExecuteAsync(zerar, new { entidadeId }, tx);

                    const string setNovo = @"
UPDATE EntidadeEndereco SET Principal = 1 
 WHERE EntidadeId = @entidadeId AND EnderecoId = @novoId;";
                    await conn.ExecuteAsync(setNovo, new { entidadeId, novoId = novoPrincipalId.Value }, tx);

                    const string atualizaEnt = @"
UPDATE Entidade SET EnderecoId = @novoId 
 WHERE Id = @entidadeId;";
                    await conn.ExecuteAsync(atualizaEnt, new { entidadeId, novoId = novoPrincipalId.Value }, tx);
                }
                else
                {
                    // não restou endereço
                    const string zeraEnt = @"
UPDATE Entidade SET EnderecoId = NULL 
 WHERE Id = @entidadeId;";
                    await conn.ExecuteAsync(zeraEnt, new { entidadeId }, tx);
                }
            }

            // 4) Se o endereço ficou “órfão” (sem vínculos), apaga de Endereco
            const string temVinculos = @"
SELECT CASE WHEN EXISTS(SELECT 1 FROM EntidadeEndereco WHERE EnderecoId = @enderecoId)
           OR EXISTS(SELECT 1 FROM PessoaEndereco   WHERE EnderecoId = @enderecoId)
           THEN 1 ELSE 0 END;";
            var aindaReferenciado = await conn.ExecuteScalarAsync<bool>(temVinculos, new { enderecoId }, tx);

            var apagouEndereco = false;
            if (!aindaReferenciado)
            {
                const string delEnd = @"DELETE FROM Endereco WHERE Id = @enderecoId;";
                await conn.ExecuteAsync(delEnd, new { enderecoId }, tx);
                apagouEndereco = true;
            }

            tx.Commit();
            return apagouEndereco;
        }
    }
}