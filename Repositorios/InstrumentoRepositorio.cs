using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Linq;
using Dapper;
using Financeiro.Models;
using Financeiro.Models.ViewModels;
using Financeiro.Infraestrutura;
using System;

namespace Financeiro.Repositorios
{
    /// <summary>
    /// Repositório do Instrumento (com lógica de Vigência, Isolamento por Entidade e Paginação).
    /// </summary>
    public class InstrumentoRepositorio : IInstrumentoRepositorio
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public InstrumentoRepositorio(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        private async Task GarantirVigenciaUnicaAsync(IDbConnection conn, IDbTransaction tx, int entidadeId, int? ignorarId = null)
        {
            const string sql = @"
                UPDATE dbo.Instrumento
                   SET Vigente = 0
                 WHERE EntidadeId = @entidadeId
                   AND Vigente = 1
                   AND (@ignorarId IS NULL OR Id <> @ignorarId);";

            await conn.ExecuteAsync(sql, new { entidadeId, ignorarId }, tx);
        }

        public async Task InserirAsync(InstrumentoViewModel vm)
        {
            using var conn = _connectionFactory.CreateConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                if (vm.Vigente)
                {
                    await GarantirVigenciaUnicaAsync(conn, tx, vm.EntidadeId);
                }

                const string sql = @"
                    INSERT INTO dbo.Instrumento
                        (Numero, Valor, Objeto, DataInicio, DataFim, Ativo, Vigente, Observacao, DataAssinatura, EntidadeId, Saldo)
                    VALUES
                        (@Numero, @Valor, @Objeto, @DataInicio, @DataFim, @Ativo, @Vigente, @Observacao, @DataAssinatura, @EntidadeId, @Saldo);";

                var parametros = new
                {
                    vm.Numero,
                    vm.Valor,
                    vm.Objeto,
                    vm.DataInicio,
                    vm.DataFim,
                    vm.Ativo,
                    vm.Vigente,
                    vm.Observacao,
                    vm.DataAssinatura,
                    vm.EntidadeId,
                    Saldo = vm.Valor
                };

                await conn.ExecuteAsync(sql, parametros, tx);
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public async Task AtualizarAsync(int id, InstrumentoViewModel vm)
        {
            using var conn = _connectionFactory.CreateConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                if (vm.Vigente)
                {
                    await GarantirVigenciaUnicaAsync(conn, tx, vm.EntidadeId, id);
                }

                const string sql = @"
                    UPDATE dbo.Instrumento
                       SET Numero         = @Numero,
                           Valor          = @Valor,
                           Objeto         = @Objeto,
                           DataInicio     = @DataInicio,
                           DataFim        = @DataFim,
                           Ativo          = @Ativo,
                           Vigente        = @Vigente,
                           Observacao     = @Observacao,
                           DataAssinatura = @DataAssinatura,
                           EntidadeId     = @EntidadeId
                     WHERE Id = @Id;";

                await conn.ExecuteAsync(sql, new
                {
                    vm.Numero,
                    vm.Valor,
                    vm.Objeto,
                    vm.DataInicio,
                    vm.DataFim,
                    vm.Ativo,
                    vm.Vigente,
                    vm.Observacao,
                    vm.DataAssinatura,
                    vm.EntidadeId,
                    Id = id
                }, tx);

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public async Task<Instrumento?> ObterPorIdAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            const string sql = "SELECT * FROM dbo.Instrumento WHERE Id = @id;";
            return await conn.QueryFirstOrDefaultAsync<Instrumento>(sql, new { id });
        }

        public async Task<IEnumerable<Instrumento>> ListarAsync()
        {
            using var conn = _connectionFactory.CreateConnection();
            const string sql = "SELECT * FROM dbo.Instrumento ORDER BY DataInicio DESC;";
            return await conn.QueryAsync<Instrumento>(sql);
        }

        public async Task<Instrumento?> ObterVigentePorEntidadeAsync(int entidadeId)
        {
            using var conn = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT TOP 1 * FROM dbo.Instrumento
                 WHERE EntidadeId = @entidadeId
                   AND Vigente = 1;";
            return await conn.QueryFirstOrDefaultAsync<Instrumento>(sql, new { entidadeId });
        }

        public async Task ExcluirAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                await conn.ExecuteAsync("DELETE FROM dbo.RecebimentoInstrumento WHERE InstrumentoId = @id;", new { id }, tx);
                await conn.ExecuteAsync("DELETE FROM dbo.InstrumentoVersao WHERE InstrumentoId = @id;", new { id }, tx);
                await conn.ExecuteAsync("DELETE FROM dbo.Instrumento WHERE Id = @id;", new { id }, tx);
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        // --- CORREÇÃO IMPORTANTE: Adicionado entidadeId para validar unicidade por escopo ---
        public async Task<bool> ExisteNumeroAsync(string numero, int entidadeId, int? ignorarId = null)
        {
            using var conn = _connectionFactory.CreateConnection();
            
            const string sql = @"
                SELECT 1 FROM dbo.Instrumento
                 WHERE Numero = @numero
                   AND EntidadeId = @entidadeId  -- <--- Filtra pela entidade
                   AND (@ignorarId IS NULL OR Id <> @ignorarId);";
            
            var existe = await conn.QueryFirstOrDefaultAsync<int?>(sql, new { numero, entidadeId, ignorarId });
            return existe.HasValue;
        }

        public async Task<string> SugerirProximoNumeroAsync(int ano)
        {
            using var conn = _connectionFactory.CreateConnection();
            const string sqlPadraoAno = @"
                SELECT MAX(TRY_CONVERT(int, LEFT(Numero, NULLIF(CHARINDEX('/', Numero), 0) - 1)))
                  FROM dbo.Instrumento
                 WHERE Numero LIKE '%/' + CAST(@ano AS varchar(4));";

            var maxComAno = await conn.QueryFirstOrDefaultAsync<int?>(sqlPadraoAno, new { ano });
            if (maxComAno.HasValue) return $"{maxComAno.Value + 1}/{ano}";

            const string sqlInteiro = "SELECT MAX(TRY_CONVERT(int, Numero)) FROM dbo.Instrumento;";
            var maxInteiro = await conn.QueryFirstOrDefaultAsync<int?>(sqlInteiro);
            return maxInteiro.HasValue ? (maxInteiro.Value + 1).ToString() : $"1/{ano}";
        }

        public async Task<(IEnumerable<InstrumentoResumoViewModel> Itens, int TotalItens)> ListarResumoPaginadoAsync(int entidadeId, int pagina, int tamanhoPagina)
        {
            using var conn = _connectionFactory.CreateConnection();

            const string sqlCount = "SELECT COUNT(*) FROM dbo.Instrumento WHERE EntidadeId = @entidadeId";
            var total = await conn.ExecuteScalarAsync<int>(sqlCount, new { entidadeId });

            const string sql = @"
                SELECT
                    i.Id                                AS InstrumentoId,
                    i.Numero                            AS Numero,
                    i.Objeto                            AS Descricao,
                    i.Vigente                           AS Vigente,
                    COALESCE(v.VigenciaInicio, i.DataInicio) AS VigenciaInicio,
                    COALESCE(v.VigenciaFim,    i.DataFim)    AS VigenciaFimAtual,
                    CAST(COALESCE(v.Valor, i.Valor) AS DECIMAL(18,2)) AS ValorTotalAtual,
                    DATEDIFF(MONTH, COALESCE(v.VigenciaInicio, i.DataInicio), COALESCE(v.VigenciaFim, i.DataFim)) + 1 AS MesesVigentesAtuais,
                    CAST(
                        COALESCE(v.Valor, i.Valor) / NULLIF(DATEDIFF(MONTH, COALESCE(v.VigenciaInicio, i.DataInicio), COALESCE(v.VigenciaFim, i.DataFim)) + 1, 0)
                        AS DECIMAL(18,2)
                    ) AS ValorMensalAtual,
                    CAST(i.Saldo AS DECIMAL(18,2))     AS SaldoAtual
                FROM dbo.Instrumento i
                OUTER APPLY (
                    SELECT TOP 1 iv.Valor, iv.VigenciaInicio, iv.VigenciaFim
                    FROM dbo.InstrumentoVersao iv
                    WHERE iv.InstrumentoId = i.Id
                    ORDER BY 
                        CASE WHEN iv.VigenciaFim IS NULL THEN 1 ELSE 0 END DESC,
                        iv.Versao DESC,
                        iv.DataRegistro DESC
                ) v
                WHERE i.EntidadeId = @entidadeId
                ORDER BY i.Id DESC
                OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY;";

            var skip = (pagina - 1) * tamanhoPagina;
            var itens = await conn.QueryAsync<InstrumentoResumoViewModel>(sql, new { entidadeId, skip, take = tamanhoPagina });

            return (itens ?? Enumerable.Empty<InstrumentoResumoViewModel>(), total);
        }

        public async Task<InstrumentoResumoViewModel?> ObterResumoAsync(int instrumentoId)
        {
            using var conn = _connectionFactory.CreateConnection();

            const string sqlResumo = @"
                SELECT TOP 1
                    i.Id                                AS InstrumentoId,
                    i.Numero                            AS Numero,
                    i.Objeto                            AS Descricao,
                    i.Vigente                           AS Vigente,
                    COALESCE(v.VigenciaInicio, i.DataInicio) AS VigenciaInicio,
                    COALESCE(v.VigenciaFim,    i.DataFim)    AS VigenciaFimAtual,
                    CAST(COALESCE(v.Valor, i.Valor) AS DECIMAL(18,2)) AS ValorTotalAtual,
                    DATEDIFF(MONTH, COALESCE(v.VigenciaInicio, i.DataInicio), COALESCE(v.VigenciaFim, i.DataFim)) + 1 AS MesesVigentesAtuais,
                    CAST(
                        COALESCE(v.Valor, i.Valor) / NULLIF(DATEDIFF(MONTH, COALESCE(v.VigenciaInicio, i.DataInicio), COALESCE(v.VigenciaFim, i.DataFim)) + 1, 0)
                        AS DECIMAL(18,2)
                    ) AS ValorMensalAtual,
                    CAST(i.Saldo AS DECIMAL(18,2))     AS SaldoAtual
                FROM dbo.Instrumento i
                OUTER APPLY (
                    SELECT TOP 1 iv.Valor, iv.VigenciaInicio, iv.VigenciaFim
                    FROM dbo.InstrumentoVersao iv
                    WHERE iv.InstrumentoId = i.Id
                    ORDER BY 
                        CASE WHEN iv.VigenciaFim IS NULL THEN 1 ELSE 0 END DESC,
                        iv.Versao DESC,
                        iv.DataRegistro DESC
                ) v
                WHERE i.Id = @instrumentoId";

            var resumo = await conn.QueryFirstOrDefaultAsync<InstrumentoResumoViewModel>(sqlResumo, new { instrumentoId });
            if (resumo == null) return null;

            const string sqlMes = @"
                DECLARE @InicioMesAtual DATE = DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1);
                DECLARE @FimMesAtual DATE = EOMONTH(GETDATE());

                SELECT SUM(Valor) 
                FROM dbo.RecebimentoInstrumento
                WHERE InstrumentoId = @instrumentoId 
                AND DataInicio <= @FimMesAtual 
                AND DataFim >= @InicioMesAtual;";
            
            var totalRecebidoMes = await conn.QuerySingleOrDefaultAsync<decimal?>(sqlMes, new { instrumentoId });

            resumo.TotalLancadoNoMes = totalRecebidoMes ?? 0;
            resumo.SaldoDoMesAtual = resumo.ValorMensalAtual - resumo.TotalLancadoNoMes;

            return resumo;
        }
        public async Task<IEnumerable<dynamic>> ListarInstrumentosParaSelectAsync()
        {
            // Esta query retorna apenas os dados essenciais para um SelectList/Select2
            const string sql = "SELECT Id, Numero AS Text FROM Instrumento WHERE Ativo = 1 ORDER BY Numero";
            
            using var conn = _connectionFactory.CreateConnection();
            // Dapper mapeia automaticamente para { Id, Text }
            return await conn.QueryAsync<dynamic>(sql);
        }
    }
}