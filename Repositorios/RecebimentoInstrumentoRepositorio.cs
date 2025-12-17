using Dapper;
using Financeiro.Infraestrutura;
using Financeiro.Models.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Financeiro.Repositorios
{
    public class RecebimentoInstrumentoRepositorio : IRecebimentoInstrumentoRepositorio
    {
        private readonly IDbConnectionFactory _factory;
        public RecebimentoInstrumentoRepositorio(IDbConnectionFactory factory) => _factory = factory;

        public async Task<int> InserirAsync(RecebimentoViewModel vm)
        {
            const string sql = @"
                INSERT INTO dbo.RecebimentoInstrumento 
                    (InstrumentoId, Valor, DataInicio, DataFim, Observacao, DataCriacao)
                VALUES 
                    (@InstrumentoId, @Valor, @DataInicio, @DataFim, @Observacao, GETDATE());
                SELECT CAST(SCOPE_IDENTITY() as int);";

            using var conn = _factory.CreateConnection();
            return await conn.QuerySingleAsync<int>(sql, vm);
        }

        public async Task AtualizarAsync(RecebimentoViewModel vm)
        {
            const string sql = @"
                UPDATE dbo.RecebimentoInstrumento SET
                    Valor = @Valor,
                    DataInicio = @DataInicio,
                    DataFim = @DataFim,
                    Observacao = @Observacao
                WHERE Id = @Id;";

            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(sql, vm);
        }

        public async Task ExcluirAsync(int id)
        {
            // Exclusão física (conforme estrutura original)
            const string sql = "DELETE FROM dbo.RecebimentoInstrumento WHERE Id = @Id;";
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(sql, new { Id = id });
        }

        public async Task<RecebimentoViewModel?> ObterParaEdicaoAsync(int id)
        {
            const string sql = "SELECT * FROM dbo.RecebimentoInstrumento WHERE Id = @Id;";
            using var conn = _factory.CreateConnection();
            return await conn.QuerySingleOrDefaultAsync<RecebimentoViewModel>(sql, new { Id = id });
        }

        public async Task<(IEnumerable<RecebimentoViewModel> Itens, int Total)> ListarPaginadoPorInstrumentoAsync(int instrumentoId, int pagina, int tamanho)
        {
            var p = pagina < 1 ? 1 : pagina;
            var offset = (p - 1) * tamanho;

            const string sql = @"
                SELECT r.*, i.Numero as InstrumentoNumero
                FROM dbo.RecebimentoInstrumento r
                JOIN dbo.Instrumento i ON r.InstrumentoId = i.Id
                WHERE r.InstrumentoId = @instrumentoId
                ORDER BY r.DataInicio DESC
                OFFSET @Offset ROWS FETCH NEXT @Tamanho ROWS ONLY;

                SELECT COUNT(*) FROM dbo.RecebimentoInstrumento WHERE InstrumentoId = @instrumentoId;";

            using var conn = _factory.CreateConnection();
            using var multi = await conn.QueryMultipleAsync(sql, new { instrumentoId, Offset = offset, Tamanho = tamanho });

            var itens = await multi.ReadAsync<RecebimentoViewModel>();
            var total = await multi.ReadFirstAsync<int>();

            return (itens, total);
        }

        public async Task<IEnumerable<RecebimentoViewModel>> ListarTodosAsync()
        {
            const string sql = @"
                SELECT r.*, i.Numero as InstrumentoNumero
                FROM dbo.RecebimentoInstrumento r
                JOIN dbo.Instrumento i ON r.InstrumentoId = i.Id
                ORDER BY r.DataInicio DESC;";
            
            using var conn = _factory.CreateConnection();
            return await conn.QueryAsync<RecebimentoViewModel>(sql);
        }
    }
}