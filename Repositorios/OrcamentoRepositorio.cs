using Dapper;
using Financeiro.Infraestrutura;
using Financeiro.Models;
using Financeiro.Models.ViewModels;
using Microsoft.Data.SqlClient; 
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Financeiro.Repositorios
{
    public class OrcamentoRepositorio : IOrcamentoRepositorio
    {
        private readonly IDbConnectionFactory _factory;

        public OrcamentoRepositorio(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<(IEnumerable<OrcamentoListViewModel> Itens, int TotalItens)> ListarPaginadoAsync(int entidadeId, int pagina, int tamanhoPagina)
        {
            using var conn = _factory.CreateConnection();

            // [CORREÇÃO AQUI]: O subselect agora soma pela tabela ContratoItem + OrcamentoDetalhe
            const string sqlHeader = @"
                SELECT 
                    o.Id, 
                    o.Nome, 
                    o.Observacao, 
                    o.VigenciaInicio, 
                    o.VigenciaFim,
                    o.ValorPrevistoTotal, 
                    o.Ativo,
                    i.Numero + ' - ' + i.Objeto AS InstrumentoNome,
                    
                    ISNULL((
                        SELECT SUM(ci.Valor) 
                        FROM ContratoItem ci
                        INNER JOIN OrcamentoDetalhe od ON ci.OrcamentoDetalheId = od.Id
                        INNER JOIN Contrato c ON ci.ContratoId = c.Id
                        WHERE od.OrcamentoId = o.Id AND c.Ativo = 1
                    ), 0) AS ValorComprometido

                FROM [dbo].[Orcamento] o
                INNER JOIN [dbo].[Instrumento] i ON o.InstrumentoId = i.Id
                WHERE i.EntidadeId = @entidadeId
                ORDER BY o.Id DESC
                OFFSET @Offset ROWS FETCH NEXT @TamanhoPagina ROWS ONLY;

                SELECT COUNT(*) 
                FROM [dbo].[Orcamento] o
                INNER JOIN [dbo].[Instrumento] i ON o.InstrumentoId = i.Id
                WHERE i.EntidadeId = @entidadeId;";

            using var multi = await conn.QueryMultipleAsync(sqlHeader, new 
            { 
                entidadeId, 
                Offset = (pagina - 1) * tamanhoPagina, 
                TamanhoPagina = tamanhoPagina 
            });

            var orcamentos = (await multi.ReadAsync<OrcamentoListViewModel>()).ToList();
            var totalItens = await multi.ReadSingleAsync<int>();

            if (!orcamentos.Any()) return (orcamentos, totalItens);

            // --- LÓGICA DE BI (DETALHAMENTO) ---
            // Também precisa ser ajustada para não usar c.OrcamentoId
            var idsOrcamentos = orcamentos.Select(o => o.Id).ToList();

            const string sqlBI = @"
                SELECT 
                    od.OrcamentoId,
                    od.Nome as NomeItem,
                    SUM(ci.Valor) as ValorConsumido
                FROM ContratoItem ci
                INNER JOIN OrcamentoDetalhe od ON ci.OrcamentoDetalheId = od.Id
                INNER JOIN Contrato c ON ci.ContratoId = c.Id
                WHERE od.OrcamentoId IN @Ids 
                  AND c.Ativo = 1
                GROUP BY od.OrcamentoId, od.Nome
                ORDER BY ValorConsumido DESC";

            var detalhesRaw = await conn.QueryAsync<dynamic>(sqlBI, new { Ids = idsOrcamentos });

            foreach (var orcamento in orcamentos)
            {
                var itensDesteOrcamento = detalhesRaw.Where(x => x.OrcamentoId == orcamento.Id);

                foreach (var item in itensDesteOrcamento)
                {
                    decimal valor = (decimal)item.ValorConsumido;
                    decimal percent = orcamento.ValorPrevistoTotal > 0 
                                      ? (valor / orcamento.ValorPrevistoTotal) * 100 
                                      : 0;

                    orcamento.DetalhamentoConsumo.Add(new OrcamentoDetalheBI
                    {
                        NomeItem = item.NomeItem,
                        ValorConsumido = valor,
                        PercentualDoTotal = percent
                    });
                }
            }

            return (orcamentos, totalItens);
        }

        public async Task InserirAsync(OrcamentoViewModel vm)
        {
            using var conn = _factory.CreateConnection();
            conn.Open();
            using var transaction = conn.BeginTransaction();

            try
            {
                const string sqlHeader = @"
                    INSERT INTO Orcamento (InstrumentoId, Nome, VigenciaInicio, VigenciaFim, ValorPrevistoTotal, Ativo, DataCriacao, Observacao)
                    VALUES (@InstrumentoId, @Nome, @VigenciaInicio, @VigenciaFim, @ValorPrevistoTotal, @Ativo, @DataCriacao, @Observacao);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";
                
                var orcamentoId = await conn.QuerySingleAsync<int>(sqlHeader, new 
                {
                    vm.InstrumentoId, vm.Nome, vm.VigenciaInicio, vm.VigenciaFim,
                    vm.ValorPrevistoTotal, vm.Ativo, DataCriacao = DateTime.Now, vm.Observacao
                }, transaction);

                vm.Id = orcamentoId;
                await InserirDetalhesRecursivo(conn, transaction, orcamentoId, null, vm.Detalhamento);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task AtualizarAsync(int id, OrcamentoViewModel vm)
        {
            using var conn = _factory.CreateConnection();
            conn.Open();
            using var transaction = conn.BeginTransaction();

            try
            {
                const string sqlHeader = @"
                    UPDATE Orcamento SET
                        InstrumentoId = @InstrumentoId,
                        Nome = @Nome,
                        VigenciaInicio = @VigenciaInicio,
                        VigenciaFim = @VigenciaFim,
                        ValorPrevistoTotal = @ValorPrevistoTotal,
                        Ativo = @Ativo,
                        Observacao = @Observacao
                    WHERE Id = @Id;";
                
                await conn.ExecuteAsync(sqlHeader, new 
                { 
                    vm.InstrumentoId, vm.Nome, vm.VigenciaInicio, vm.VigenciaFim, 
                    vm.ValorPrevistoTotal, vm.Ativo, vm.Observacao, Id = id 
                }, transaction);

                var idsParaManter = ObterIdsRecursivos(vm.Detalhamento);

                if (idsParaManter.Any())
                {
                    const string sqlDeleteObsoletos = "DELETE FROM OrcamentoDetalhe WHERE OrcamentoId = @OrcamentoId AND Id NOT IN @Ids;";
                    await conn.ExecuteAsync(sqlDeleteObsoletos, new { OrcamentoId = id, Ids = idsParaManter }, transaction);
                }
                else
                {
                    const string sqlDeleteAll = "DELETE FROM OrcamentoDetalhe WHERE OrcamentoId = @OrcamentoId;";
                    await conn.ExecuteAsync(sqlDeleteAll, new { OrcamentoId = id }, transaction);
                }

                await UpsertDetalhesRecursivo(conn, transaction, id, null, vm.Detalhamento);

                transaction.Commit();
            }
            catch (SqlException ex)
            {
                transaction.Rollback();
                if (ex.Number == 547) 
                {
                    throw new Exception("Não é possível remover itens que possuem contratos vinculados. Inative o item ou remova os contratos primeiro.");
                }
                throw;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private async Task InserirDetalhesRecursivo(IDbConnection conn, IDbTransaction transaction, int orcamentoId, int? parentId, List<OrcamentoDetalheViewModel> detalhes)
        {
            if (detalhes == null || !detalhes.Any()) return;

            const string sqlDetalhe = @"
                INSERT INTO OrcamentoDetalhe (OrcamentoId, ParentId, Nome, ValorPrevisto, PermiteLancamento)
                VALUES (@OrcamentoId, @ParentId, @Nome, @ValorPrevisto, @PermiteLancamento);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            foreach (var detalhe in detalhes)
            {
                var newParentId = await conn.QuerySingleAsync<int>(sqlDetalhe, new
                {
                    OrcamentoId = orcamentoId,
                    ParentId = parentId,
                    detalhe.Nome,
                    detalhe.ValorPrevisto,
                    PermiteLancamento = (detalhe.Filhos == null || !detalhe.Filhos.Any())
                }, transaction);
                
                if (detalhe.Filhos != null && detalhe.Filhos.Any())
                {
                    await InserirDetalhesRecursivo(conn, transaction, orcamentoId, newParentId, detalhe.Filhos);
                }
            }
        }

        private async Task UpsertDetalhesRecursivo(IDbConnection conn, IDbTransaction transaction, int orcamentoId, int? parentId, List<OrcamentoDetalheViewModel> detalhes)
        {
            if (detalhes == null || !detalhes.Any()) return;

            foreach (var detalhe in detalhes)
            {
                int currentId;

                if (detalhe.Id.HasValue && detalhe.Id.Value > 0)
                {
                    const string sqlUpdate = @"
                        UPDATE OrcamentoDetalhe SET 
                            ParentId = @ParentId,
                            Nome = @Nome, 
                            ValorPrevisto = @ValorPrevisto, 
                            PermiteLancamento = @PermiteLancamento
                        WHERE Id = @Id AND OrcamentoId = @OrcamentoId";

                    await conn.ExecuteAsync(sqlUpdate, new
                    {
                        Id = detalhe.Id.Value,
                        OrcamentoId = orcamentoId,
                        ParentId = parentId,
                        detalhe.Nome,
                        detalhe.ValorPrevisto,
                        PermiteLancamento = (detalhe.Filhos == null || !detalhe.Filhos.Any())
                    }, transaction);

                    currentId = detalhe.Id.Value;
                }
                else 
                {
                    const string sqlInsert = @"
                        INSERT INTO OrcamentoDetalhe (OrcamentoId, ParentId, Nome, ValorPrevisto, PermiteLancamento)
                        VALUES (@OrcamentoId, @ParentId, @Nome, @ValorPrevisto, @PermiteLancamento);
                        SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    currentId = await conn.QuerySingleAsync<int>(sqlInsert, new
                    {
                        OrcamentoId = orcamentoId,
                        ParentId = parentId,
                        detalhe.Nome,
                        detalhe.ValorPrevisto,
                        PermiteLancamento = (detalhe.Filhos == null || !detalhe.Filhos.Any())
                    }, transaction);
                }

                if (detalhe.Filhos != null && detalhe.Filhos.Any())
                {
                    await UpsertDetalhesRecursivo(conn, transaction, orcamentoId, currentId, detalhe.Filhos);
                }
            }
        }

        private List<int> ObterIdsRecursivos(List<OrcamentoDetalheViewModel> detalhes)
        {
            var ids = new List<int>();
            if (detalhes == null) return ids;

            foreach (var item in detalhes)
            {
                if (item.Id.HasValue && item.Id.Value > 0)
                {
                    ids.Add(item.Id.Value);
                }
                if (item.Filhos != null && item.Filhos.Any())
                {
                    ids.AddRange(ObterIdsRecursivos(item.Filhos));
                }
            }
            return ids;
        }

        public async Task<IEnumerable<OrcamentoListViewModel>> ListarAtivosPorEntidadeAsync(int entidadeId)
        {
            using var conn = _factory.CreateConnection();
            const string sql = @"
                SELECT o.Id, o.Nome, o.ValorPrevistoTotal, o.VigenciaInicio, o.VigenciaFim
                FROM Orcamento o
                INNER JOIN Instrumento i ON o.InstrumentoId = i.Id
                WHERE i.EntidadeId = @entidadeId AND o.Ativo = 1
                ORDER BY o.Nome";
            return await conn.QueryAsync<OrcamentoListViewModel>(sql, new { entidadeId });
        }

        public async Task<Orcamento?> ObterHeaderPorIdAsync(int id)
        {
            const string sql = "SELECT * FROM Orcamento WHERE Id = @id;";
            using var conn = _factory.CreateConnection();
            return await conn.QuerySingleOrDefaultAsync<Orcamento>(sql, new { id });
        }
        
        public async Task<IEnumerable<OrcamentoDetalhe>> ObterDetalhesPorOrcamentoIdAsync(int orcamentoId)
        {
            const string sql = "SELECT * FROM OrcamentoDetalhe WHERE OrcamentoId = @orcamentoId;";
            using var conn = _factory.CreateConnection();
            return await conn.QueryAsync<OrcamentoDetalhe>(sql, new { orcamentoId });
        }

        public async Task<decimal> ObterTotalComprometidoPorInstrumentoAsync(int instrumentoId, int? ignorarOrcamentoId = null)
        {
            using var conn = _factory.CreateConnection();
            const string sql = @"
                SELECT SUM(ValorPrevistoTotal) FROM Orcamento 
                WHERE InstrumentoId = @instrumentoId AND Ativo = 1 AND (@ignorarId IS NULL OR Id <> @ignorarId)";
            var total = await conn.ExecuteScalarAsync<decimal?>(sql, new { instrumentoId, ignorarId = ignorarOrcamentoId });
            return total ?? 0m;
        }

        public async Task ExcluirAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            conn.Open();
            using var transaction = conn.BeginTransaction();
            try
            {
                await conn.ExecuteAsync("DELETE FROM OrcamentoDetalhe WHERE OrcamentoId = @id;", new { id }, transaction);
                await conn.ExecuteAsync("DELETE FROM Orcamento WHERE Id = @id;", new { id }, transaction);
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<OrcamentoDetalhe?> ObterDetalhePorIdAsync(int id)
        {
            const string sql = "SELECT * FROM OrcamentoDetalhe WHERE Id = @id";
            using var conn = _factory.CreateConnection();
            return await conn.QuerySingleOrDefaultAsync<OrcamentoDetalhe>(sql, new { id });
        }

        public async Task<IEnumerable<OrcamentoDetalhe>> ListarDetalhesParaLancamentoAsync(int orcamentoId)
        {
            const string sql = @"
                SELECT * FROM OrcamentoDetalhe 
                WHERE OrcamentoId = @orcamentoId AND PermiteLancamento = 1
                ORDER BY Nome";
            using var conn = _factory.CreateConnection();
            return await conn.QueryAsync<OrcamentoDetalhe>(sql, new { orcamentoId });
        }
    }
}