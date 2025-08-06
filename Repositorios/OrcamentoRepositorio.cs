using Dapper;
using Financeiro.Infraestrutura;
using Financeiro.Models;
using Financeiro.Models.ViewModels;
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

        public async Task<IEnumerable<Orcamento>> ListarAsync()
        {
            const string sql = "SELECT * FROM Orcamento ORDER BY VigenciaInicio DESC;";
            using var conn = _factory.CreateConnection();
            return await conn.QueryAsync<Orcamento>(sql);
        }

        public async Task<Orcamento?> ObterHeaderPorIdAsync(int id)
        {
            const string sql = "SELECT * FROM Orcamento WHERE Id = @id;";
            using var conn = _factory.CreateConnection();
            return await conn.QuerySingleOrDefaultAsync<Orcamento>(sql, new { id });
        }

        public async Task<IEnumerable<OrcamentoDetalhe>> ObterDetalhesPorOrcamentoIdAsync(int orcamentoId)
        {
            const string sql = @"
                WITH DetalhesHierarquia AS (
                    SELECT * FROM OrcamentoDetalhe WHERE OrcamentoId = @orcamentoId AND ParentId IS NULL
                    UNION ALL
                    SELECT d.* FROM OrcamentoDetalhe d
                    INNER JOIN DetalhesHierarquia h ON d.ParentId = h.Id
                )
                SELECT * FROM DetalhesHierarquia;";
            using var conn = _factory.CreateConnection();
            return await conn.QueryAsync<OrcamentoDetalhe>(sql, new { orcamentoId });
        }

        public async Task InserirAsync(OrcamentoViewModel vm)
        {
            using var conn = _factory.CreateConnection();
            conn.Open();
            using var transaction = conn.BeginTransaction();

            try
            {
                const string sqlHeader = @"
                    INSERT INTO Orcamento (Nome, TipoAcordoId, VigenciaInicio, VigenciaFim, ValorPrevistoTotal, Ativo, Observacao)
                    VALUES (@Nome, @TipoAcordoId, @VigenciaInicio, @VigenciaFim, @ValorPrevistoTotal, @Ativo, @Observacao);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";
                var orcamentoId = await conn.QuerySingleAsync<int>(sqlHeader, vm, transaction);

                await InserirDetalhesRecursivo(conn, transaction, orcamentoId, null, vm.Detalhamento);

                transaction.Commit();
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
                    detalhe.PermiteLancamento
                }, transaction);
                
                if (detalhe.Filhos != null && detalhe.Filhos.Any())
                {
                    await InserirDetalhesRecursivo(conn, transaction, orcamentoId, newParentId, detalhe.Filhos);
                }
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
                        Nome = @Nome,
                        TipoAcordoId = @TipoAcordoId,
                        VigenciaInicio = @VigenciaInicio,
                        VigenciaFim = @VigenciaFim,
                        ValorPrevistoTotal = @ValorPrevistoTotal,
                        Ativo = @Ativo,
                        Observacao = @Observacao
                    WHERE Id = @Id;";
                
                await conn.ExecuteAsync(sqlHeader, new 
                { 
                    vm.Nome,
                    vm.TipoAcordoId,
                    vm.VigenciaInicio,
                    vm.VigenciaFim,
                    vm.ValorPrevistoTotal,
                    vm.Ativo,
                    vm.Observacao,
                    Id = id 
                }, transaction);

                const string sqlDeleteDetalhes = "DELETE FROM OrcamentoDetalhe WHERE OrcamentoId = @Id;";
                await conn.ExecuteAsync(sqlDeleteDetalhes, new { Id = id }, transaction);

                await InserirDetalhesRecursivo(conn, transaction, id, null, vm.Detalhamento);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task ExcluirAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            conn.Open();
            using var transaction = conn.BeginTransaction();

            try
            {
                const string sqlDetalhes = "DELETE FROM OrcamentoDetalhe WHERE OrcamentoId = @id;";
                await conn.ExecuteAsync(sqlDetalhes, new { id }, transaction);

                const string sqlHeader = "DELETE FROM Orcamento WHERE Id = @id;";
                await conn.ExecuteAsync(sqlHeader, new { id }, transaction);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}