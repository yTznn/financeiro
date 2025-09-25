// Repositorios/LancamentoInstrumentoRepositorio.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Financeiro.Infraestrutura;
using Financeiro.Models;

namespace Financeiro.Repositorios
{
    public class LancamentoInstrumentoRepositorio : ILancamentoInstrumentoRepositorio
    {
        private readonly IDbConnectionFactory _factory;
        public LancamentoInstrumentoRepositorio(IDbConnectionFactory factory) => _factory = factory;

        public async Task InserirAsync(LancamentoInstrumento m)
        {
            var comp = new DateTime(m.Competencia.Year, m.Competencia.Month, 1);

            const string sqlIns = @"
INSERT INTO dbo.LancamentoInstrumento(InstrumentoId, Competencia, Valor, Observacao)
VALUES (@InstrumentoId, @Competencia, @Valor, @Observacao);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            const string sqlSaldo = @"UPDATE dbo.Instrumento SET Saldo = Saldo - @Valor WHERE Id = @InstrumentoId;";

            using var conn = _factory.CreateConnection();
            if (conn.State != System.Data.ConnectionState.Open) conn.Open();
            using var tx = conn.BeginTransaction();

            var novoId = await conn.ExecuteScalarAsync<int>(sqlIns,
                new { m.InstrumentoId, Competencia = comp, m.Valor, m.Observacao }, tx);

            await conn.ExecuteAsync(sqlSaldo, new { m.Valor, m.InstrumentoId }, tx);

            tx.Commit();
            m.Id = novoId;
        }

        public async Task AtualizarAsync(int id, decimal valor, string? observacao)
        {
            const string sqlGet = @"SELECT TOP 1 * FROM dbo.LancamentoInstrumento WHERE Id=@id;";
            const string sqlUpd = @"UPDATE dbo.LancamentoInstrumento SET Valor=@valor, Observacao=@observacao WHERE Id=@id;";
            const string sqlSaldo = @"UPDATE dbo.Instrumento SET Saldo = Saldo - @delta WHERE Id = @inst;";

            using var conn = _factory.CreateConnection();
            if (conn.State != System.Data.ConnectionState.Open) conn.Open();
            using var tx = conn.BeginTransaction();

            var antigo = await conn.QueryFirstOrDefaultAsync<LancamentoInstrumento>(sqlGet, new { id }, tx);
            if (antigo is null) { tx.Rollback(); return; }

            // delta a aplicar no saldo do Instrumento
            var delta = valor - antigo.Valor;

            await conn.ExecuteAsync(sqlUpd, new { id, valor, observacao }, tx);
            if (delta != 0)
                await conn.ExecuteAsync(sqlSaldo, new { delta, inst = antigo.InstrumentoId }, tx);

            tx.Commit();
        }

        public async Task ExcluirAsync(int id)
        {
            const string sqlGet = @"SELECT TOP 1 * FROM dbo.LancamentoInstrumento WHERE Id=@id;";
            const string sqlDel = @"DELETE FROM dbo.LancamentoInstrumento WHERE Id=@id;";
            const string sqlSaldo = @"UPDATE dbo.Instrumento SET Saldo = Saldo + @valor WHERE Id = @inst;";

            using var conn = _factory.CreateConnection();
            if (conn.State != System.Data.ConnectionState.Open) conn.Open();
            using var tx = conn.BeginTransaction();

            var antigo = await conn.QueryFirstOrDefaultAsync<LancamentoInstrumento>(sqlGet, new { id }, tx);
            if (antigo is null) { tx.Rollback(); return; }

            await conn.ExecuteAsync(sqlDel, new { id }, tx);
            await conn.ExecuteAsync(sqlSaldo, new { valor = antigo.Valor, inst = antigo.InstrumentoId }, tx);

            tx.Commit();
        }

        public async Task<LancamentoInstrumento?> ObterPorIdAsync(int id)
        {
            const string sql = @"SELECT TOP 1 * FROM dbo.LancamentoInstrumento WHERE Id=@id;";
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<LancamentoInstrumento>(sql, new { id });
        }

        public async Task<IEnumerable<LancamentoInstrumento>> ListarPorInstrumentoAsync(
            int instrumentoId, DateTime? de = null, DateTime? ate = null)
        {
            const string sql = @"
SELECT *
  FROM dbo.LancamentoInstrumento
 WHERE InstrumentoId = @instrumentoId
   AND (@de  IS NULL OR Competencia >= @de)
   AND (@ate IS NULL OR Competencia <= @ate)
 ORDER BY Competencia DESC, Id DESC;";

            using var conn = _factory.CreateConnection();
            return await conn.QueryAsync<LancamentoInstrumento>(sql, new { instrumentoId, de, ate });
        }

        public async Task<decimal> SomatorioNoMesAsync(int instrumentoId, DateTime competenciaPrimeiroDia)
        {
            const string sql = @"
SELECT SUM(Valor)
  FROM dbo.LancamentoInstrumento
 WHERE InstrumentoId = @instrumentoId
   AND Competencia   = @comp;";

            var comp = new DateTime(competenciaPrimeiroDia.Year, competenciaPrimeiroDia.Month, 1);
            using var conn = _factory.CreateConnection();
            return await conn.ExecuteScalarAsync<decimal?>(sql, new { instrumentoId, comp }) ?? 0m;
        }
    }
}