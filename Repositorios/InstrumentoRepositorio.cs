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
    /// Repositório da “faixa nova” de Instrumento (mapeando a tabela atual TipoAcordo).
    /// </summary>
    public class InstrumentoRepositorio : IInstrumentoRepositorio
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public InstrumentoRepositorio(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task InserirAsync(TipoAcordoViewModel vm)
        {
            using var conn = _connectionFactory.CreateConnection();
            const string sql = @"
                INSERT INTO TipoAcordo
                    (Numero, Valor, Objeto, DataInicio, DataFim, Ativo, Observacao, DataAssinatura, EntidadeId)
                VALUES
                    (@Numero, @Valor, @Objeto, @DataInicio, @DataFim, @Ativo, @Observacao, @DataAssinatura, @EntidadeId)";
            await conn.ExecuteAsync(sql, vm);
        }

        public async Task AtualizarAsync(int id, TipoAcordoViewModel vm)
        {
            using var conn = _connectionFactory.CreateConnection();
            const string sql = @"
                UPDATE TipoAcordo
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

        public async Task<TipoAcordo?> ObterPorIdAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            const string sql = "SELECT * FROM TipoAcordo WHERE Id = @id";
            return await conn.QueryFirstOrDefaultAsync<TipoAcordo>(sql, new { id });
        }

        public async Task<IEnumerable<TipoAcordo>> ListarAsync()
        {
            using var conn = _connectionFactory.CreateConnection();
            const string sql = "SELECT * FROM TipoAcordo ORDER BY DataInicio DESC";
            return await conn.QueryAsync<TipoAcordo>(sql);
        }

        /// <summary>
        /// Exclui filhos e depois o Instrumento (mantém comportamento atual).
        /// </summary>
        public async Task ExcluirAsync(int id)
        {
            using var conn = _connectionFactory.CreateConnection();
            if (conn.State != ConnectionState.Open) conn.Open();
            using var tx = conn.BeginTransaction();

            const string delOrcDet = @"
                DELETE od
                FROM OrcamentoDetalhe od
                INNER JOIN Orcamento o ON o.Id = od.OrcamentoId
                WHERE o.TipoAcordoId = @id;";

            const string delOrc = @"
                DELETE FROM Orcamento
                WHERE TipoAcordoId = @id;";

            const string delVersao = @"
                DELETE FROM AcordoVersao
                WHERE TipoAcordoId = @id;";

            const string delAcordo = @"
                DELETE FROM TipoAcordo
                WHERE Id = @id;";

            await conn.ExecuteAsync(delOrcDet, new { id }, tx);
            await conn.ExecuteAsync(delOrc, new { id }, tx);
            await conn.ExecuteAsync(delVersao, new { id }, tx);
            await conn.ExecuteAsync(delAcordo, new { id }, tx);

            tx.Commit();
        }

        public async Task<bool> ExisteNumeroAsync(string numero, int? ignorarId = null)
        {
            using var conn = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT 1
                FROM TipoAcordo
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
                FROM TipoAcordo
                WHERE Numero LIKE '%/' + CAST(@ano AS varchar(4))";
            var maxComAno = await conn.QueryFirstOrDefaultAsync<int?>(sqlPadraoAno, new { ano });

            if (maxComAno.HasValue)
                return $"{maxComAno.Value + 1}/{ano}";

            // fallback: se não há padrão com ano, tenta inteiro puro
            const string sqlInteiro = @"SELECT MAX(TRY_CONVERT(int, Numero)) FROM TipoAcordo";
            var maxInteiro = await conn.QueryFirstOrDefaultAsync<int?>(sqlInteiro);

            return maxInteiro.HasValue ? (maxInteiro.Value + 1).ToString() : $"1/{ano}";
        }
    }
}