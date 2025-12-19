using Dapper;
using Financeiro.Infraestrutura;
using Financeiro.Models;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Linq;

namespace Financeiro.Repositorios
{
    public class ContratoVersaoRepositorio : IContratoVersaoRepositorio
    {
        private readonly IDbConnectionFactory _factory;

        public ContratoVersaoRepositorio(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<int> InserirAsync(ContratoVersao versao)
        {
            const string sql = @"
                INSERT INTO ContratoVersao 
                    (ContratoId, Versao, ObjetoContrato, DataInicio, DataFim, ValorContrato, TipoAditivo, Observacao, DataRegistro, DataInicioAditivo, Ativo)
                VALUES 
                    (@ContratoId, @Versao, @ObjetoContrato, @DataInicio, @DataFim, @ValorContrato, @TipoAditivo, @Observacao, @DataRegistro, @DataInicioAditivo, @Ativo);
                SELECT CAST(SCOPE_IDENTITY() as int);";

            using var conn = _factory.CreateConnection();
            return await conn.QuerySingleAsync<int>(sql, versao);
        }

        public async Task AtualizarAsync(ContratoVersao versao)
        {
            const string sql = @"
                UPDATE ContratoVersao 
                SET DataInicio = @DataInicio, 
                    DataFim = @DataFim, 
                    ValorContrato = @ValorContrato,
                    Ativo = @Ativo
                WHERE Id = @Id";
            
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(sql, versao);
        }

        public async Task ExcluirAsync(int id)
        {
            // Apaga itens primeiro (CASCADE manual por segurança)
            await ExcluirItensPorVersaoAsync(id);
            
            const string sql = "DELETE FROM ContratoVersao WHERE Id = @id";
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(sql, new { id });
        }

        // --- MÉTODOS DE ITENS (SNAPSHOT) ---

        public async Task InserirItensAsync(List<ContratoVersaoItem> itens)
        {
            const string sql = @"
                INSERT INTO ContratoVersaoItem (ContratoVersaoId, OrcamentoDetalheId, Valor) 
                VALUES (@ContratoVersaoId, @OrcamentoDetalheId, @Valor)";

            using var conn = _factory.CreateConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                await conn.ExecuteAsync(sql, itens, tx);
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public async Task ExcluirItensPorVersaoAsync(int contratoVersaoId)
        {
            const string sql = "DELETE FROM ContratoVersaoItem WHERE ContratoVersaoId = @id";
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(sql, new { id = contratoVersaoId });
        }

        public async Task<IEnumerable<ContratoVersaoItem>> ListarItensPorVersaoAsync(int contratoVersaoId)
        {
            // Fazemos o JOIN para trazer o Nome do Item (OrcamentoDetalhe)
            // Isso é útil para exibir o histórico na tela ("O que era antes?")
            const string sql = @"
                SELECT 
                    cvi.Id, 
                    cvi.ContratoVersaoId, 
                    cvi.OrcamentoDetalheId, 
                    cvi.Valor,
                    od.Nome AS NomeItem
                FROM ContratoVersaoItem cvi
                INNER JOIN OrcamentoDetalhe od ON cvi.OrcamentoDetalheId = od.Id
                WHERE cvi.ContratoVersaoId = @contratoVersaoId";

            using var conn = _factory.CreateConnection();
            return await conn.QueryAsync<ContratoVersaoItem>(sql, new { contratoVersaoId });
        }

        // --- CONSULTAS PADRÃO ---

        public async Task<ContratoVersao?> ObterVersaoAtualAsync(int contratoId)
        {
            const string sql = "SELECT TOP 1 * FROM ContratoVersao WHERE ContratoId = @contratoId ORDER BY Versao DESC";
            using var conn = _factory.CreateConnection();
            return await conn.QuerySingleOrDefaultAsync<ContratoVersao>(sql, new { contratoId });
        }

        public async Task<ContratoVersao?> ObterPorIdAsync(int contratoId, int versao)
        {
             const string sql = "SELECT * FROM ContratoVersao WHERE ContratoId = @contratoId AND Versao = @versao";
             using var conn = _factory.CreateConnection();
             return await conn.QuerySingleOrDefaultAsync<ContratoVersao>(sql, new { contratoId, versao });
        }

        public async Task<IEnumerable<ContratoVersao>> ListarPorContratoAsync(int contratoId)
        {
            const string sql = "SELECT * FROM ContratoVersao WHERE ContratoId = @contratoId ORDER BY Versao DESC";
            using var conn = _factory.CreateConnection();
            return await conn.QueryAsync<ContratoVersao>(sql, new { contratoId });
        }

        public async Task<(IEnumerable<ContratoVersao> Itens, int TotalPaginas)> ListarPaginadoAsync(int contratoId, int pagina, int tamanhoPagina = 5)
        {
            const string sqlItens = @"
                SELECT * FROM ContratoVersao 
                WHERE ContratoId = @contratoId 
                ORDER BY Versao DESC
                OFFSET @Offset ROWS FETCH NEXT @TamanhoPagina ROWS ONLY";

            const string sqlCount = "SELECT COUNT(*) FROM ContratoVersao WHERE ContratoId = @contratoId";

            using var conn = _factory.CreateConnection();
            using var multi = await conn.QueryMultipleAsync($"{sqlItens}; {sqlCount}", new
            {
                contratoId,
                Offset = (pagina - 1) * tamanhoPagina,
                TamanhoPagina = tamanhoPagina
            });

            var itens = await multi.ReadAsync<ContratoVersao>();
            var totalItens = await multi.ReadSingleAsync<int>();
            var totalPaginas = (int)System.Math.Ceiling(totalItens / (double)tamanhoPagina);

            return (itens, totalPaginas);
        }

        public async Task<int> ContarPorContratoAsync(int contratoId)
        {
            const string sql = "SELECT COUNT(*) FROM ContratoVersao WHERE ContratoId = @contratoId";
            using var conn = _factory.CreateConnection();
            return await conn.ExecuteScalarAsync<int>(sql, new { contratoId });
        }
    }
}