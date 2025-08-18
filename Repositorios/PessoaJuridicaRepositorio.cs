// Repositorios/PessoaJuridicaRepositorio.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Financeiro.Infraestrutura;
using Financeiro.Models;
using Financeiro.Models.ViewModels;

namespace Financeiro.Repositorios
{
    public class PessoaJuridicaRepositorio : IPessoaJuridicaRepositorio
    {
        private readonly IDbConnectionFactory _factory;
        public PessoaJuridicaRepositorio(IDbConnectionFactory factory) => _factory = factory;

        public async Task InserirAsync(PessoaJuridicaViewModel vm)
        {
            const string sql = @"
        INSERT INTO PessoaJuridica
            (RazaoSocial, NomeFantasia, NumeroInscricao, Email, Telefone, SituacaoAtiva)
        VALUES(@RazaoSocial, @NomeFantasia, @NumeroInscricao, @Email, @Telefone, @SituacaoAtiva);";

            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(sql, vm);
        }

        public async Task<IEnumerable<PessoaJuridica>> ListarAsync()
        {
            const string sql = "SELECT * FROM PessoaJuridica ORDER BY Id DESC;";
            using var conn = _factory.CreateConnection();
            return await conn.QueryAsync<PessoaJuridica>(sql);
        }

        /* ---------- NOVOS MÉTODOS ---------- */

        public async Task<PessoaJuridica?> ObterPorIdAsync(int id)
        {
            const string sql = "SELECT * FROM PessoaJuridica WHERE Id = @Id;";
            using var conn = _factory.CreateConnection();
            return await conn.QuerySingleOrDefaultAsync<PessoaJuridica>(sql, new { Id = id });
        }

        public async Task AtualizarAsync(int id, PessoaJuridicaViewModel vm)
        {
            const string sql = @"
        UPDATE PessoaJuridica SET
            RazaoSocial     = @RazaoSocial,
            NomeFantasia    = @NomeFantasia,
            NumeroInscricao = @NumeroInscricao,
            Email           = @Email,
            Telefone        = @Telefone,
            SituacaoAtiva   = @SituacaoAtiva
        WHERE Id = @Id;";

            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(sql, new
            {
                Id = id,
                vm.RazaoSocial,
                vm.NomeFantasia,
                vm.NumeroInscricao,
                vm.Email,
                vm.Telefone,
                vm.SituacaoAtiva
            });
        }
        public async Task ExcluirAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                // ========== 1) PessoaEndereco (schema pode variar) ==========
                // SQL dinâmico para evitar "Nome de coluna inválido" em colunas que não existam
                var sqlDelPessoaEndereco = @"
        DECLARE @sql nvarchar(max) = N'';

        IF OBJECT_ID('dbo.PessoaEndereco','U') IS NOT NULL
        BEGIN
            IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.PessoaEndereco') AND name = 'PessoaJuridicaId')
                SET @sql = N'DELETE FROM dbo.PessoaEndereco WHERE PessoaJuridicaId = @Id;';
            ELSE IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.PessoaEndereco') AND name = 'PessoaId')
                SET @sql = N'DELETE FROM dbo.PessoaEndereco WHERE PessoaId = @Id;';
            ELSE IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.PessoaEndereco') AND name = 'PJId')
                SET @sql = N'DELETE FROM dbo.PessoaEndereco WHERE PJId = @Id;';

            IF (@sql <> N'')
                EXEC sp_executesql @sql, N'@Id int', @Id = @Id;
        END
        ";
                await conn.ExecuteAsync(sqlDelPessoaEndereco, new { Id = id }, tx);

                // ========== 2) PessoaConta (FK_PessoaConta_PJ) ==========
                // Aqui sabemos pelo erro que a coluna PessoaJuridicaId existe. Pode deletar direto.
                const string delPessoaConta = @"
        IF OBJECT_ID('dbo.PessoaConta','U') IS NOT NULL
            DELETE FROM dbo.PessoaConta WHERE PessoaJuridicaId = @Id;";
                await conn.ExecuteAsync(delPessoaConta, new { Id = id }, tx);

                // ========== 3) Contas bancárias (se existirem) ==========
                var sqlDelContas = @"
        DECLARE @sql nvarchar(max) = N'';

        IF OBJECT_ID('dbo.ContaBancaria','U') IS NOT NULL
        BEGIN
            IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ContaBancaria') AND name = 'PessoaJuridicaId')
                SET @sql = N'DELETE FROM dbo.ContaBancaria WHERE PessoaJuridicaId = @Id;';
            ELSE IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ContaBancaria') AND name = 'PessoaId')
                SET @sql = N'DELETE FROM dbo.ContaBancaria WHERE PessoaId = @Id;';

            IF (@sql <> N'')
                EXEC sp_executesql @sql, N'@Id int', @Id = @Id;
        END

        IF OBJECT_ID('dbo.ContaBancariaPessoaJuridica','U') IS NOT NULL
            EXEC sp_executesql N'DELETE FROM dbo.ContaBancariaPessoaJuridica WHERE PessoaJuridicaId = @Id;', N'@Id int', @Id = @Id;

        IF OBJECT_ID('dbo.PessoaJuridicaContaBancaria','U') IS NOT NULL
            EXEC sp_executesql N'DELETE FROM dbo.PessoaJuridicaContaBancaria WHERE PessoaJuridicaId = @Id;', N'@Id int', @Id = @Id;
        ";
                await conn.ExecuteAsync(sqlDelContas, new { Id = id }, tx);

                // ========== 4) Por último: apagar a PJ ==========
                const string delPJ = @"DELETE FROM dbo.PessoaJuridica WHERE Id = @Id;";
                await conn.ExecuteAsync(delPJ, new { Id = id }, tx);

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public async Task<PessoaJuridica?> ObterPorCnpjAsync(string cnpj)
        {
            const string sql = @"
        SELECT * 
        FROM PessoaJuridica
        WHERE NumeroInscricao = @Cnpj;";

            using var conn = _factory.CreateConnection();
            return await conn.QuerySingleOrDefaultAsync<PessoaJuridica>(sql, new { Cnpj = cnpj });
        }

        public async Task<(IEnumerable<PessoaJuridica> Lista, int TotalRegistros)> ListarPaginadoAsync(int paginaAtual, int itensPorPagina)
        {
            const string sqlLista = @"
                SELECT * FROM PessoaJuridica
                ORDER BY Id DESC
                OFFSET @Offset ROWS FETCH NEXT @Limite ROWS ONLY;";

            const string sqlTotal = "SELECT COUNT(*) FROM PessoaJuridica;";

            using var conn = _factory.CreateConnection();

            var offset = (paginaAtual - 1) * itensPorPagina;

            var lista = await conn.QueryAsync<PessoaJuridica>(sqlLista, new { Offset = offset, Limite = itensPorPagina });
            var total = await conn.ExecuteScalarAsync<int>(sqlTotal);

            return (lista, total);
        }
        // Adicione este método no final da classe PessoaJuridicaRepositorio:

        public async Task<bool> ExisteContratoPorPessoaJuridicaAsync(int pessoaJuridicaId)
        {
            const string sql = "SELECT 1 FROM Contrato WHERE PessoaJuridicaId = @pessoaJuridicaId";
            using var conn = _factory.CreateConnection();
            var result = await conn.QueryFirstOrDefaultAsync<int?>(sql, new { pessoaJuridicaId });
            return result.HasValue;
        }
    }
}