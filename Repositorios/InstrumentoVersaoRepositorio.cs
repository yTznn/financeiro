using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Financeiro.Infraestrutura;
using Financeiro.Models;

namespace Financeiro.Repositorios
{
    /// <summary>
    /// Repositório de versões/aditivos do Instrumento.
    /// Mapeia dbo.InstrumentoVersao (InstrumentoId).
    /// </summary>
    public class InstrumentoVersaoRepositorio : IInstrumentoVersaoRepositorio
    {
        private readonly IDbConnectionFactory _factory;

        public InstrumentoVersaoRepositorio(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task InserirAsync(InstrumentoVersao versao)
        {
            using var conn = _factory.CreateConnection();
            if (conn.State != System.Data.ConnectionState.Open) conn.Open();
            using var tx = conn.BeginTransaction();

            // 1) Valor vigente anterior: versão vigente ou valor base do instrumento
            const string sqlValorAnterior = @"
        SELECT TOP 1 v.Valor
        FROM dbo.InstrumentoVersao v
        WHERE v.InstrumentoId = @InstrumentoId
        AND v.VigenciaFim IS NULL
        ORDER BY v.Versao DESC;

        IF @@ROWCOUNT = 0
            SELECT i.Valor
            FROM dbo.Instrumento i
            WHERE i.Id = @InstrumentoId;";

            // O batch acima pode retornar duas result sets dependendo do provedor; para simplificar:
            // Vamos tentar a vigente; se null, pegamos do instrumento.
            const string sqlVigente = @"
        SELECT TOP 1 v.Valor
        FROM dbo.InstrumentoVersao v
        WHERE v.InstrumentoId = @InstrumentoId
        AND v.VigenciaFim IS NULL
        ORDER BY v.Versao DESC;";

            var valorAnterior = await conn.QueryFirstOrDefaultAsync<decimal?>(sqlVigente, new { versao.InstrumentoId }, tx)
                            ?? await conn.QueryFirstAsync<decimal>("SELECT Valor FROM dbo.Instrumento WHERE Id = @Id", new { Id = versao.InstrumentoId }, tx);

            // 2) Insere a versão/aditivo
            const string sqlInsert = @"
        INSERT INTO dbo.InstrumentoVersao
            (InstrumentoId, Versao, VigenciaInicio, VigenciaFim,
            Valor, Objeto, TipoAditivo, Observacao, DataAssinatura, DataRegistro)
        VALUES
            (@InstrumentoId, @Versao, @VigenciaInicio, @VigenciaFim,
            @Valor, @Objeto, @TipoAditivo, @Observacao, @DataAssinatura, @DataRegistro);";

            await conn.ExecuteAsync(sqlInsert, versao, tx);

            // 3) Δ e ajuste de Saldo
            var delta = versao.Valor - valorAnterior;
            if (delta != 0)
            {
                await conn.ExecuteAsync(
                    "UPDATE dbo.Instrumento SET Saldo = Saldo + @delta WHERE Id = @id;",
                    new { delta, id = versao.InstrumentoId }, tx);
            }

            tx.Commit();
        }


        public async Task<IEnumerable<InstrumentoVersao>> ListarPorInstrumentoAsync(int instrumentoId)
        {
            const string sql = @"
SELECT *
FROM dbo.InstrumentoVersao
WHERE InstrumentoId = @instrumentoId
ORDER BY Versao DESC;";

            using var conn = _factory.CreateConnection();
            return await conn.QueryAsync<InstrumentoVersao>(sql, new { instrumentoId });
        }

        public async Task<InstrumentoVersao?> ObterVersaoAtualAsync(int instrumentoId)
        {
            const string sql = @"
        SELECT TOP 1 *
        FROM dbo.InstrumentoVersao
        WHERE InstrumentoId = @instrumentoId
        ORDER BY 
            CASE WHEN VigenciaFim IS NULL THEN 1 ELSE 0 END DESC,
            Versao DESC,
            DataRegistro DESC;";

            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<InstrumentoVersao>(sql, new { instrumentoId });
        }


        public async Task<(IEnumerable<InstrumentoVersao> Itens, int TotalPaginas)> ListarPaginadoAsync(int instrumentoId, int pagina, int tamPag = 5)
        {
            const string sqlItens = @"
SELECT *
FROM dbo.InstrumentoVersao
WHERE InstrumentoId = @instrumentoId
ORDER BY Versao DESC
OFFSET (@pagina-1) * @tam ROWS FETCH NEXT @tam ROWS ONLY;";

            const string sqlCount = @"
SELECT COUNT(*)
FROM dbo.InstrumentoVersao
WHERE InstrumentoId = @instrumentoId;";

            using var conn = _factory.CreateConnection();
            var itens = await conn.QueryAsync<InstrumentoVersao>(sqlItens, new { instrumentoId, pagina, tam = tamPag });
            int total = await conn.ExecuteScalarAsync<int>(sqlCount, new { instrumentoId });
            int totalPaginas = (int)Math.Ceiling(total / (double)tamPag);
            return (itens, totalPaginas);
        }

        public async Task ExcluirAsync(int versaoId)
        {
            using var conn = _factory.CreateConnection();
            if (conn.State != System.Data.ConnectionState.Open) conn.Open();
            using var tx = conn.BeginTransaction();

            // 1) Pega a versão a excluir
            var atual = await conn.QueryFirstOrDefaultAsync<(int InstrumentoId, int Versao, decimal Valor)>(
                "SELECT InstrumentoId, Versao, Valor FROM dbo.InstrumentoVersao WHERE Id = @id;",
                new { id = versaoId }, tx);

            if (atual.InstrumentoId == 0)
            {
                // não achou — apenas tenta excluir e sair
                await conn.ExecuteAsync("DELETE FROM dbo.InstrumentoVersao WHERE Id = @id;", new { id = versaoId }, tx);
                tx.Commit();
                return;
            }

            // 2) Valor anterior à versão removida
            var anterior = await conn.QueryFirstOrDefaultAsync<decimal?>(
                @"SELECT TOP 1 Valor
                    FROM dbo.InstrumentoVersao
                WHERE InstrumentoId = @inst AND Versao < @versao
                ORDER BY Versao DESC;",
                new { inst = atual.InstrumentoId, versao = atual.Versao }, tx)
                ?? await conn.QueryFirstAsync<decimal>(
                    "SELECT Valor FROM dbo.Instrumento WHERE Id = @id;",
                    new { id = atual.InstrumentoId }, tx);

            // 3) Exclui
            await conn.ExecuteAsync("DELETE FROM dbo.InstrumentoVersao WHERE Id = @id;", new { id = versaoId }, tx);

            // 4) Δ reverso e ajuste de Saldo
            var deltaReverso = -(atual.Valor - anterior); // reverte o efeito daquela versão
            if (deltaReverso != 0)
            {
                await conn.ExecuteAsync(
                    "UPDATE dbo.Instrumento SET Saldo = Saldo + @delta WHERE Id = @id;",
                    new { delta = deltaReverso, id = atual.InstrumentoId }, tx);
            }

            tx.Commit();
        }


        public async Task<InstrumentoVersao?> ObterVersaoAnteriorAsync(int instrumentoId, int versaoAtual)
        {
            const string sql = @"
SELECT TOP 1 *
FROM dbo.InstrumentoVersao
WHERE InstrumentoId = @instrumentoId
  AND Versao < @versaoAtual
ORDER BY Versao DESC;";
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<InstrumentoVersao>(sql, new { instrumentoId, versaoAtual });
        }

        public async Task AtualizarVigenciaFimAsync(int versaoId, DateTime? dataFim)
        {
            const string sql = @"UPDATE dbo.InstrumentoVersao SET VigenciaFim = @dataFim WHERE Id = @versaoId;";
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(sql, new { versaoId, dataFim });
        }

        public async Task AtualizarDetalhesAsync(int versaoId, decimal novoValor, TipoAditivo tipoAditivo, string? observacao, DateTime? dataAssinatura)
        {
            using var conn = _factory.CreateConnection();
            if (conn.State != System.Data.ConnectionState.Open) conn.Open();
            using var tx = conn.BeginTransaction();

            // 1) Valor antes
            var antes = await conn.QueryFirstOrDefaultAsync<(int InstrumentoId, decimal Valor)>(
                "SELECT InstrumentoId, Valor FROM dbo.InstrumentoVersao WHERE Id = @id;",
                new { id = versaoId }, tx);

            // 2) Atualiza a versão
            const string sql = @"
        UPDATE dbo.InstrumentoVersao
        SET Valor = @novoValor,
            TipoAditivo = @tipoAditivo,
            Observacao = @observacao,
            DataAssinatura = @dataAssinatura
        WHERE Id = @versaoId;";
            await conn.ExecuteAsync(sql, new { versaoId, novoValor, tipoAditivo, observacao, dataAssinatura }, tx);

            // 3) Δ e ajuste de Saldo
            var delta = novoValor - antes.Valor;
            if (delta != 0)
            {
                await conn.ExecuteAsync(
                    "UPDATE dbo.Instrumento SET Saldo = Saldo + @delta WHERE Id = @instrumentoId;",
                    new { delta, instrumentoId = antes.InstrumentoId }, tx);
            }

            tx.Commit();
        }
        // ✅ Novo método exigido pela interface
        public async Task<InstrumentoVersao?> ObterPorIdAsync(int versaoId)
        {
            using var conn = _factory.CreateConnection();
            const string sql = @"
SELECT TOP 1
       Id,
       InstrumentoId,
       Versao,
       Valor,
       TipoAditivo,          -- int ou varchar conforme seu schema
       VigenciaInicio,
       VigenciaFim,
       Observacao,
       DataAssinatura
  FROM dbo.InstrumentoVersao
 WHERE Id = @versaoId;";
            return await conn.QueryFirstOrDefaultAsync<InstrumentoVersao>(sql, new { versaoId });
        }
    }
}