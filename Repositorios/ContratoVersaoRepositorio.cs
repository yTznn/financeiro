using Dapper;
using Financeiro.Infraestrutura;
using Financeiro.Models;
using System;
using System.Collections.Generic;
using System.Data; // Importante para Transaction
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

        // [ALTERADO] Agora retorna int (o ID da versão criada)
        public async Task<int> InserirAsync(ContratoVersao versao)
        {
            const string sql = @"
                INSERT INTO ContratoVersao 
                    (ContratoId, Versao, ObjetoContrato, DataInicio, DataFim, ValorContrato, TipoAditivo, Observacao, DataRegistro, DataInicioAditivo)
                VALUES 
                    (@ContratoId, @Versao, @ObjetoContrato, @DataInicio, @DataFim, @ValorContrato, @TipoAditivo, @Observacao, @DataRegistro, @DataInicioAditivo);
                SELECT CAST(SCOPE_IDENTITY() as int);";

            using var conn = _factory.CreateConnection();
            return await conn.QuerySingleAsync<int>(sql, versao);
        }

        // [NOVO] Método para salvar o "detalhe" (rateio) da versão
        public async Task InserirNaturezasHistoricoAsync(int contratoVersaoId, IEnumerable<ContratoVersaoNatureza> itens)
        {
            const string sql = @"
                INSERT INTO ContratoVersaoNatureza (ContratoVersaoId, NaturezaId, Valor)
                VALUES (@ContratoVersaoId, @NaturezaId, @Valor)";

            using var conn = _factory.CreateConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            
            try
            {
                foreach (var item in itens)
                {
                    // Garante o vínculo correto
                    item.ContratoVersaoId = contratoVersaoId; 
                    await conn.ExecuteAsync(sql, item, trans);
                }
                trans.Commit();
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        public async Task<ContratoVersao?> ObterVersaoAtualAsync(int contratoId)
        {
            const string sql = @"
                SELECT TOP 1 * FROM ContratoVersao
                WHERE ContratoId = @contratoId
                ORDER BY Versao DESC;";

            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<ContratoVersao>(sql, new { contratoId });
        }

        public async Task<IEnumerable<ContratoVersao>> ListarPorContratoAsync(int contratoId)
        {
            const string sql = @"
                SELECT * FROM ContratoVersao
                WHERE ContratoId = @contratoId
                ORDER BY Versao DESC;";

            using var conn = _factory.CreateConnection();
            return await conn.QueryAsync<ContratoVersao>(sql, new { contratoId });
        }

        public async Task<(IEnumerable<ContratoVersao> Itens, int TotalPaginas)> ListarPaginadoAsync(int contratoId, int pagina, int tamanhoPagina = 5)
        {
            const string sqlItens = @"
                SELECT * FROM ContratoVersao
                WHERE ContratoId = @contratoId
                ORDER BY Versao DESC
                OFFSET @Offset ROWS FETCH NEXT @TamanhoPagina ROWS ONLY;";

            const string sqlCount = @"
                SELECT COUNT(*) FROM ContratoVersao
                WHERE ContratoId = @contratoId;";

            using var conn = _factory.CreateConnection();
            
            var multi = await conn.QueryMultipleAsync($"{sqlItens} {sqlCount}", new
            {
                contratoId,
                Offset = (pagina - 1) * tamanhoPagina,
                TamanhoPagina = tamanhoPagina
            });

            var itens = await multi.ReadAsync<ContratoVersao>();
            var totalItens = await multi.ReadSingleAsync<int>();
            var totalPaginas = (int)Math.Ceiling(totalItens / (double)tamanhoPagina);

            return (itens, totalPaginas);
        }

        public async Task<int> ContarPorContratoAsync(int contratoId)
        {
            const string sql = "SELECT COUNT(*) FROM ContratoVersao WHERE ContratoId = @contratoId;";
            using var conn = _factory.CreateConnection();
            return await conn.ExecuteScalarAsync<int>(sql, new { contratoId });
        }

        public async Task ExcluirAsync(int versaoId)
        {
            // [ALTERADO] Precisa apagar os filhos (Naturezas) antes do pai (Versão)
            const string sqlFilhos = "DELETE FROM ContratoVersaoNatureza WHERE ContratoVersaoId = @versaoId;";
            const string sqlPai = "DELETE FROM ContratoVersao WHERE Id = @versaoId;";
            
            using var conn = _factory.CreateConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                await conn.ExecuteAsync(sqlFilhos, new { versaoId }, trans);
                await conn.ExecuteAsync(sqlPai, new { versaoId }, trans);
                trans.Commit();
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        public async Task RestaurarContratoAPartirDaVersaoAsync(ContratoVersao versaoAnterior)
        {
            // 1. Restaura o Cabeçalho do Contrato
            const string sqlHeader = @"
                UPDATE Contrato
                SET  ValorContrato  = @ValorContrato,
                     DataFim        = @DataFim,
                     DataInicio     = @DataInicio, -- Importante restaurar inicio também caso tenha mudado
                     ObjetoContrato = @ObjetoContrato
                WHERE Id = @ContratoId;";

            // 2. Restaura as Naturezas (Apaga as atuais do contrato e insere as do histórico)
            // Note que aqui pegamos da tabela ContratoVersaoNatureza e jogamos na ContratoNatureza
            const string sqlRestaurarNaturezas = @"
                DELETE FROM ContratoNatureza WHERE ContratoId = @ContratoId;

                INSERT INTO ContratoNatureza (ContratoId, NaturezaId, Valor)
                SELECT @ContratoId, NaturezaId, Valor
                FROM ContratoVersaoNatureza
                WHERE ContratoVersaoId = @VersaoId;";

            using var conn = _factory.CreateConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                // Restaura Header
                await conn.ExecuteAsync(sqlHeader, new
                {
                    versaoAnterior.ValorContrato,
                    versaoAnterior.DataFim,
                    versaoAnterior.DataInicio,
                    versaoAnterior.ObjetoContrato,
                    versaoAnterior.ContratoId
                }, trans);

                // Restaura Rateio
                await conn.ExecuteAsync(sqlRestaurarNaturezas, new 
                { 
                    ContratoId = versaoAnterior.ContratoId, 
                    VersaoId = versaoAnterior.Id // ID da linha na tabela ContratoVersao
                }, trans);

                trans.Commit();
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }
        public async Task<IEnumerable<ContratoNatureza>> ListarNaturezasPorContratoAsync(int contratoId)
        {
            const string sql = "SELECT * FROM ContratoNatureza WHERE ContratoId = @contratoId;";
            
            using var conn = _factory.CreateConnection();
            return await conn.QueryAsync<ContratoNatureza>(sql, new { contratoId });
        }
        public async Task RecriarNaturezasHistoricoAsync(int contratoVersaoId, IEnumerable<ContratoVersaoNatureza> novosItens)
        {
            using var conn = _factory.CreateConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                // 1. Limpa os itens antigos dessa versão específica (o snapshot "errado")
                const string sqlDelete = "DELETE FROM ContratoVersaoNatureza WHERE ContratoVersaoId = @contratoVersaoId;";
                await conn.ExecuteAsync(sqlDelete, new { contratoVersaoId }, trans);

                // 2. Insere os novos itens corrigidos
                const string sqlInsert = @"
                    INSERT INTO ContratoVersaoNatureza (ContratoVersaoId, NaturezaId, Valor)
                    VALUES (@ContratoVersaoId, @NaturezaId, @Valor)";

                foreach (var item in novosItens)
                {
                    item.ContratoVersaoId = contratoVersaoId; // Garante amarração
                    await conn.ExecuteAsync(sqlInsert, item, trans);
                }

                trans.Commit();
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }
    }
}