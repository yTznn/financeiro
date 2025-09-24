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
                    (Numero, Valor, Objeto, DataInicio, DataFim, Ativo, Observacao, DataAssinatura, EntidadeId)
                VALUES
                    (@Numero, @Valor, @Objeto, @DataInicio, @DataFim, @Ativo, @Observacao, @DataAssinatura, @EntidadeId)";
            await conn.ExecuteAsync(sql, vm);
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
                WHERE Id = @Id";
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
            const string sql = "SELECT * FROM dbo.Instrumento WHERE Id = @id";
            return await conn.QueryFirstOrDefaultAsync<Instrumento>(sql, new { id });
        }

        public async Task<IEnumerable<Instrumento>> ListarAsync()
        {
            using var conn = _connectionFactory.CreateConnection();
            const string sql = "SELECT * FROM dbo.Instrumento ORDER BY DataInicio DESC";
            return await conn.QueryAsync<Instrumento>(sql);
        }

        /// <summary>
        /// Exclui filhos e depois o Instrumento, em transação.
        /// </summary>
        public async Task ExcluirAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            if (conn.State != ConnectionState.Open) conn.Open();
            using var tx = conn.BeginTransaction();

            // Se o seu schema mantiver vínculo de orçamento com instrumento:
            const string delOrcDet = @"
                DELETE od
                FROM dbo.OrcamentoDetalhe od
                INNER JOIN dbo.Orcamento o ON o.Id = od.OrcamentoId
                WHERE o.InstrumentoId = @id;";

            const string delOrc = @"
                DELETE FROM dbo.Orcamento
                WHERE InstrumentoId = @id;";

            const string delVersao = @"
                DELETE FROM dbo.InstrumentoVersao
                WHERE InstrumentoId = @id;";

            const string delInstrumento = @"
                DELETE FROM dbo.Instrumento
                WHERE Id = @id;";

            await conn.ExecuteAsync(delOrcDet, new { id }, tx);
            await conn.ExecuteAsync(delOrc, new { id }, tx);
            await conn.ExecuteAsync(delVersao, new { id }, tx);
            await conn.ExecuteAsync(delInstrumento, new { id }, tx);

            tx.Commit();
        }

        public async Task<bool> ExisteNumeroAsync(string numero, int? ignorarId = null)
        {
            using var conn = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT 1
                FROM dbo.Instrumento
                WHERE Numero = @numero
                  AND (@ignorarId IS NULL OR Id <> @ignorarId)";
            var existe = await conn.QueryFirstOrDefaultAsync<int?>(sql, new { numero, ignorarId });
            return existe.HasValue;
        }

        public async Task<string> SugerirProximoNumeroAsync(int ano)
        {
            using var conn = _connectionFactory.CreateConnection();

            // ex.: pega o maior 'X' do padrão 'X/ANO'
            const string sqlPadraoAno = @"
                SELECT MAX(TRY_CONVERT(int, LEFT(Numero, NULLIF(CHARINDEX('/', Numero),0)-1)))
                FROM dbo.Instrumento
                WHERE Numero LIKE '%/' + CAST(@ano AS varchar(4))";
            var maxComAno = await conn.QueryFirstOrDefaultAsync<int?>(sqlPadraoAno, new { ano });

            if (maxComAno.HasValue)
                return $"{maxComAno.Value + 1}/{ano}";

            // fallback: se não há padrão com ano, tenta inteiro puro
            const string sqlInteiro = @"SELECT MAX(TRY_CONVERT(int, Numero)) FROM dbo.Instrumento";
            var maxInteiro = await conn.QueryFirstOrDefaultAsync<int?>(sqlInteiro);

            return maxInteiro.HasValue ? (maxInteiro.Value + 1).ToString() : $"1/{ano}";
        }
    }
}