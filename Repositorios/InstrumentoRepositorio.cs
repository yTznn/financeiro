using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Financeiro.Models;
using Financeiro.Models.ViewModels;
using Financeiro.Infraestrutura;

namespace Financeiro.Repositorios
{
    /// <summary>
    /// Repositório do Instrumento (antes: TipoAcordo).
    /// </summary>
    public class InstrumentoRepositorio : IInstrumentoRepositorio
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public InstrumentoRepositorio(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task InserirAsync(InstrumentoViewModel vm)
        {
            using var conn = _connectionFactory.CreateConnection();

            const string sql = @"
                INSERT INTO dbo.Instrumento
                    (Numero, Valor, Objeto, DataInicio, DataFim, Ativo, Observacao, DataAssinatura, EntidadeId, Saldo)
                VALUES
                    (@Numero, @Valor, @Objeto, @DataInicio, @DataFim, @Ativo, @Observacao, @DataAssinatura, @EntidadeId, @Saldo);";

            // Saldo inicial = Valor total (já calculado no controller conforme mensal ↔ total)
            var parametros = new
            {
                vm.Numero,
                vm.Valor,
                vm.Objeto,
                vm.DataInicio,
                vm.DataFim,
                vm.Ativo,
                vm.Observacao,
                vm.DataAssinatura,
                vm.EntidadeId,
                Saldo = vm.Valor
            };

            await conn.ExecuteAsync(sql, parametros);
        }
        public async Task AtualizarAsync(int id, InstrumentoViewModel vm)
        {
            using var conn = _connectionFactory.CreateConnection();

            const string sql = @"
                UPDATE dbo.Instrumento
                   SET Numero         = @Numero,
                       Valor          = @Valor,
                       Objeto         = @Objeto,
                       DataInicio     = @DataInicio,
                       DataFim        = @DataFim,
                       Ativo          = @Ativo,
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
                vm.Observacao,
                vm.DataAssinatura,
                vm.EntidadeId,
                Id = id
            });
        }

        public async Task<Instrumento?> ObterPorIdAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();

            const string sql = @"
                SELECT *
                  FROM dbo.Instrumento
                 WHERE Id = @id;";

            return await conn.QueryFirstOrDefaultAsync<Instrumento>(sql, new { id });
        }

        public async Task<IEnumerable<Instrumento>> ListarAsync()
        {
            using var conn = _connectionFactory.CreateConnection();

            const string sql = @"
                SELECT *
                  FROM dbo.Instrumento
              ORDER BY DataInicio DESC;";

            return await conn.QueryAsync<Instrumento>(sql);
        }

        /// <summary>
        /// Exclui filhos e depois o Instrumento, em transação.
        /// </summary>
        public async Task ExcluirAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            if (conn.State != System.Data.ConnectionState.Open) conn.Open();

            using var tx = conn.BeginTransaction();
            try
            {
                const string delLanc = "DELETE FROM dbo.LancamentoInstrumento WHERE InstrumentoId = @id;";
                const string delVersao = "DELETE FROM dbo.InstrumentoVersao WHERE InstrumentoId = @id;";
                const string delInstrumento = "DELETE FROM dbo.Instrumento WHERE Id = @id;";

                await conn.ExecuteAsync(delLanc, new { id }, tx);
                await conn.ExecuteAsync(delVersao, new { id }, tx);
                await conn.ExecuteAsync(delInstrumento, new { id }, tx);

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public async Task<bool> ExisteNumeroAsync(string numero, int? ignorarId = null)
        {
            using var conn = _connectionFactory.CreateConnection();

            const string sql = @"
                SELECT 1
                  FROM dbo.Instrumento
                 WHERE Numero = @numero
                   AND (@ignorarId IS NULL OR Id <> @ignorarId);";

            var existe = await conn.QueryFirstOrDefaultAsync<int?>(sql, new { numero, ignorarId });
            return existe.HasValue;
        }

        public async Task<string> SugerirProximoNumeroAsync(int ano)
        {
            using var conn = _connectionFactory.CreateConnection();

            // ex.: pega o maior 'X' do padrão 'X/ANO'
            const string sqlPadraoAno = @"
                SELECT MAX(TRY_CONVERT(int, LEFT(Numero, NULLIF(CHARINDEX('/', Numero), 0) - 1)))
                  FROM dbo.Instrumento
                 WHERE Numero LIKE '%/' + CAST(@ano AS varchar(4));";

            var maxComAno = await conn.QueryFirstOrDefaultAsync<int?>(sqlPadraoAno, new { ano });

            if (maxComAno.HasValue)
                return $"{maxComAno.Value + 1}/{ano}";

            // fallback: se não há padrão com ano, tenta inteiro puro
            const string sqlInteiro = @"
                SELECT MAX(TRY_CONVERT(int, Numero))
                  FROM dbo.Instrumento;";

            var maxInteiro = await conn.QueryFirstOrDefaultAsync<int?>(sqlInteiro);

            return maxInteiro.HasValue ? (maxInteiro.Value + 1).ToString() : $"1/{ano}";
        }

        public async Task<IEnumerable<InstrumentoResumoViewModel>> ListarResumoAsync()
        {
            using var conn = _connectionFactory.CreateConnection();

            const string sql = @"
        SELECT
            i.Id                                AS InstrumentoId,
            i.Numero                            AS Numero,
            i.Objeto                            AS Descricao,
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
        ORDER BY i.Id DESC;";

            var lista = await conn.QueryAsync<InstrumentoResumoViewModel>(sql);
            return lista ?? Enumerable.Empty<InstrumentoResumoViewModel>();
        }
        public async Task<InstrumentoResumoViewModel?> ObterResumoAsync(int instrumentoId)
        {
            // Esta primeira parte, que chama o ListarResumoAsync, continua correta.
            var resumo = (await ListarResumoAsync()).FirstOrDefault(r => r.InstrumentoId == instrumentoId);
            if (resumo == null) return null;

            // A correção está na consulta SQL abaixo.
            using var conn = _connectionFactory.CreateConnection();
            const string sqlMes = @"
                -- Declara as variáveis para o início e fim do mês atual
                DECLARE @InicioMesAtual DATE = DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1);
                DECLARE @FimMesAtual DATE = EOMONTH(GETDATE());

                -- Soma os valores dos recebimentos cujo período cruza com o mês atual
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
    }
}