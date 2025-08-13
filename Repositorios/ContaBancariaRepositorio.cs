using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Financeiro.Infraestrutura;
using Financeiro.Models;
using Financeiro.Models.ViewModels;

namespace Financeiro.Repositorios
{
    /// <summary>
    /// Implementação Dapper para múltiplas contas com vínculo PF/PJ em PessoaConta.
    /// Regras:
    /// - Apenas 1 principal por PF e por PJ (índices únicos filtrados garantem no banco).
    /// - DefinirPrincipal/Inserir com IsPrincipal sempre limpam as demais dentro de transação.
    /// </summary>
    public class ContaBancariaRepositorio : IContaBancariaRepositorio
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public ContaBancariaRepositorio(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        /* ===================== CONSULTAS ===================== */

        public async Task<IEnumerable<ContaBancariaViewModel>> ListarPorPessoaFisicaAsync(int pessoaFisicaId)
        {
            using var conn = _connectionFactory.CreateConnection();
            var sql = @"
SELECT 
    pc.Id               AS VinculoId,
    cb.Id               AS Id,             -- ContaBancariaId
    cb.Banco,
    cb.Agencia,
    cb.Conta,
    cb.ChavePix,
    pc.IsPrincipal,
    pc.PessoaFisicaId,
    pc.PessoaJuridicaId
FROM PessoaConta pc
JOIN ContaBancaria cb ON cb.Id = pc.ContaBancariaId
WHERE pc.PessoaFisicaId = @pessoaFisicaId
ORDER BY pc.IsPrincipal DESC, pc.Id DESC;";
            return await conn.QueryAsync<ContaBancariaViewModel>(sql, new { pessoaFisicaId });
        }

        public async Task<IEnumerable<ContaBancariaViewModel>> ListarPorPessoaJuridicaAsync(int pessoaJuridicaId)
        {
            using var conn = _connectionFactory.CreateConnection();
            var sql = @"
SELECT 
    pc.Id               AS VinculoId,
    cb.Id               AS Id,             -- ContaBancariaId
    cb.Banco,
    cb.Agencia,
    cb.Conta,
    cb.ChavePix,
    pc.IsPrincipal,
    pc.PessoaFisicaId,
    pc.PessoaJuridicaId
FROM PessoaConta pc
JOIN ContaBancaria cb ON cb.Id = pc.ContaBancariaId
WHERE pc.PessoaJuridicaId = @pessoaJuridicaId
ORDER BY pc.IsPrincipal DESC, pc.Id DESC;";
            return await conn.QueryAsync<ContaBancariaViewModel>(sql, new { pessoaJuridicaId });
        }

        public async Task<ContaBancariaViewModel?> ObterPrincipalPorPessoaFisicaAsync(int pessoaFisicaId)
        {
            using var conn = _connectionFactory.CreateConnection();
            var sql = @"
SELECT TOP 1
    pc.Id               AS VinculoId,
    cb.Id               AS Id,
    cb.Banco,
    cb.Agencia,
    cb.Conta,
    cb.ChavePix,
    pc.IsPrincipal,
    pc.PessoaFisicaId,
    pc.PessoaJuridicaId
FROM PessoaConta pc
JOIN ContaBancaria cb ON cb.Id = pc.ContaBancariaId
WHERE pc.PessoaFisicaId = @pessoaFisicaId AND pc.IsPrincipal = 1
ORDER BY pc.Id DESC;";
            return await conn.QueryFirstOrDefaultAsync<ContaBancariaViewModel>(sql, new { pessoaFisicaId });
        }

        public async Task<ContaBancariaViewModel?> ObterPrincipalPorPessoaJuridicaAsync(int pessoaJuridicaId)
        {
            using var conn = _connectionFactory.CreateConnection();
            var sql = @"
SELECT TOP 1
    pc.Id               AS VinculoId,
    cb.Id               AS Id,
    cb.Banco,
    cb.Agencia,
    cb.Conta,
    cb.ChavePix,
    pc.IsPrincipal,
    pc.PessoaFisicaId,
    pc.PessoaJuridicaId
FROM PessoaConta pc
JOIN ContaBancaria cb ON cb.Id = pc.ContaBancariaId
WHERE pc.PessoaJuridicaId = @pessoaJuridicaId AND pc.IsPrincipal = 1
ORDER BY pc.Id DESC;";
            return await conn.QueryFirstOrDefaultAsync<ContaBancariaViewModel>(sql, new { pessoaJuridicaId });
        }

        public async Task<ContaBancariaViewModel?> ObterContaPorIdAsync(int contaBancariaId)
        {
            using var conn = _connectionFactory.CreateConnection();
            var sql = @"
SELECT 
    cb.Id     AS Id,
    cb.Banco,
    cb.Agencia,
    cb.Conta,
    cb.ChavePix
FROM ContaBancaria cb
WHERE cb.Id = @contaBancariaId;";
            return await conn.QueryFirstOrDefaultAsync<ContaBancariaViewModel>(sql, new { contaBancariaId });
        }

        public async Task<ContaBancariaViewModel?> ObterVinculoPorIdAsync(int vinculoId)
        {
            using var conn = _connectionFactory.CreateConnection();
            var sql = @"
SELECT 
    pc.Id               AS VinculoId,
    cb.Id               AS Id,
    cb.Banco,
    cb.Agencia,
    cb.Conta,
    cb.ChavePix,
    pc.IsPrincipal,
    pc.PessoaFisicaId,
    pc.PessoaJuridicaId
FROM PessoaConta pc
JOIN ContaBancaria cb ON cb.Id = pc.ContaBancariaId
WHERE pc.Id = @vinculoId;";
            return await conn.QueryFirstOrDefaultAsync<ContaBancariaViewModel>(sql, new { vinculoId });
        }

        /// <summary>
        /// ✅ Compatibilidade: retorna a conta principal da PF como ENTIDADE (para controllers antigos).
        /// </summary>
        public async Task<ContaBancaria?> ObterPorPessoaFisicaAsync(int pessoaFisicaId)
        {
            using var conn = _connectionFactory.CreateConnection();
            const string sql = @"
SELECT TOP 1
    cb.Id, cb.Banco, cb.Agencia, cb.Conta, cb.ChavePix
FROM PessoaConta pc
JOIN ContaBancaria cb ON cb.Id = pc.ContaBancariaId
WHERE pc.PessoaFisicaId = @pessoaFisicaId AND pc.IsPrincipal = 1
ORDER BY pc.Id DESC;";
            return await conn.QueryFirstOrDefaultAsync<ContaBancaria>(sql, new { pessoaFisicaId });
        }

        /// <summary>
        /// ✅ Compatibilidade: retorna a conta principal da PJ como ENTIDADE (para controllers antigos).
        /// </summary>
        public async Task<ContaBancaria?> ObterPorPessoaJuridicaAsync(int pessoaJuridicaId)
        {
            using var conn = _connectionFactory.CreateConnection();
            const string sql = @"
SELECT TOP 1
    cb.Id, cb.Banco, cb.Agencia, cb.Conta, cb.ChavePix
FROM PessoaConta pc
JOIN ContaBancaria cb ON cb.Id = pc.ContaBancariaId
WHERE pc.PessoaJuridicaId = @pessoaJuridicaId AND pc.IsPrincipal = 1
ORDER BY pc.Id DESC;";
            return await conn.QueryFirstOrDefaultAsync<ContaBancaria>(sql, new { pessoaJuridicaId });
        }

        /* ===================== GRAVAÇÃO ====================== */

        public async Task<int> InserirEVincularAsync(ContaBancariaViewModel vm)
        {
            var temPJ = vm.PessoaJuridicaId.HasValue;
            var temPF = vm.PessoaFisicaId.HasValue;
            if (temPJ == temPF)
                throw new System.ArgumentException("É necessário informar exatamente um dono: PessoaJuridicaId OU PessoaFisicaId.");

            using var conn = _connectionFactory.CreateConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();

            var sqlConta = @"
INSERT INTO ContaBancaria (Banco, Agencia, Conta, ChavePix)
VALUES (@Banco, @Agencia, @Conta, @ChavePix);
SELECT CAST(SCOPE_IDENTITY() AS INT);";
            var contaId = await conn.ExecuteScalarAsync<int>(sqlConta, new
            {
                vm.Banco,
                vm.Agencia,
                vm.Conta,
                vm.ChavePix
            }, tx);

            if (vm.IsPrincipal)
            {
                if (temPJ)
                {
                    await conn.ExecuteAsync(
                        "UPDATE PessoaConta SET IsPrincipal = 0 WHERE PessoaJuridicaId = @pid",
                        new { pid = vm.PessoaJuridicaId!.Value }, tx);
                }
                else
                {
                    await conn.ExecuteAsync(
                        "UPDATE PessoaConta SET IsPrincipal = 0 WHERE PessoaFisicaId = @pid",
                        new { pid = vm.PessoaFisicaId!.Value }, tx);
                }
            }

            var sqlVinculo = @"
INSERT INTO PessoaConta (PessoaJuridicaId, PessoaFisicaId, ContaBancariaId, IsPrincipal)
VALUES (@PessoaJuridicaId, @PessoaFisicaId, @ContaBancariaId, @IsPrincipal);
SELECT CAST(SCOPE_IDENTITY() AS INT);";
            var vinculoId = await conn.ExecuteScalarAsync<int>(sqlVinculo, new
            {
                PessoaJuridicaId = vm.PessoaJuridicaId,
                PessoaFisicaId   = vm.PessoaFisicaId,
                ContaBancariaId  = contaId,
                IsPrincipal      = vm.IsPrincipal ? 1 : 0
            }, tx);

            tx.Commit();
            return vinculoId;
        }

        public async Task AtualizarContaAsync(int contaBancariaId, ContaBancariaViewModel vm)
        {
            using var conn = _connectionFactory.CreateConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();

            var sql = @"
UPDATE ContaBancaria
SET Banco=@Banco, Agencia=@Agencia, Conta=@Conta, ChavePix=@ChavePix
WHERE Id=@Id;";
            await conn.ExecuteAsync(sql, new
            {
                Id = contaBancariaId,
                vm.Banco,
                vm.Agencia,
                vm.Conta,
                vm.ChavePix
            }, tx);

            if (vm.IsPrincipal && vm.VinculoId.HasValue)
            {
                await DefinirPrincipalInternalAsync(conn, tx, vm.VinculoId.Value);
            }

            tx.Commit();
        }

        public async Task DefinirPrincipalAsync(int vinculoId)
        {
            using var conn = _connectionFactory.CreateConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();
            await DefinirPrincipalInternalAsync(conn, tx, vinculoId);
            tx.Commit();
        }

        public async Task RemoverVinculoAsync(int vinculoId, bool removerContaSeOrfa = false)
        {
            using var conn = _connectionFactory.CreateConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();

            var who = await conn.QueryFirstOrDefaultAsync<(int ContaBancariaId, int? PF, int? PJ, bool IsPrincipal)>(@"
SELECT pc.ContaBancariaId, pc.PessoaFisicaId AS PF, pc.PessoaJuridicaId AS PJ, pc.IsPrincipal
FROM PessoaConta pc
WHERE pc.Id = @vinculoId;", new { vinculoId }, tx);

            if (who.ContaBancariaId == 0 && who.PF == null && who.PJ == null)
            {
                tx.Rollback();
                return;
            }

            await conn.ExecuteAsync("DELETE FROM PessoaConta WHERE Id=@vinculoId;", new { vinculoId }, tx);

            if (removerContaSeOrfa)
            {
                var count = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(1) FROM PessoaConta WHERE ContaBancariaId=@id;",
                    new { id = who.ContaBancariaId }, tx);
                if (count == 0)
                {
                    await conn.ExecuteAsync("DELETE FROM ContaBancaria WHERE Id=@id;", new { id = who.ContaBancariaId }, tx);
                }
            }

            tx.Commit();
        }

        /* ===================== PRIVADOS ====================== */

        private static async Task DefinirPrincipalInternalAsync(IDbConnection conn, IDbTransaction tx, int vinculoId)
        {
            var dono = await conn.QueryFirstOrDefaultAsync<(int? PF, int? PJ)>(@"
SELECT PessoaFisicaId AS PF, PessoaJuridicaId AS PJ
FROM PessoaConta
WHERE Id = @vinculoId;", new { vinculoId }, tx);

            if (dono.PF is null && dono.PJ is null)
                throw new System.ArgumentException("Vínculo não encontrado para definição de principal.", nameof(vinculoId));

            if (dono.PJ.HasValue)
            {
                await conn.ExecuteAsync("UPDATE PessoaConta SET IsPrincipal = 0 WHERE PessoaJuridicaId = @pid;",
                    new { pid = dono.PJ.Value }, tx);
            }
            else
            {
                await conn.ExecuteAsync("UPDATE PessoaConta SET IsPrincipal = 0 WHERE PessoaFisicaId = @pid;",
                    new { pid = dono.PF!.Value }, tx);
            }

            await conn.ExecuteAsync("UPDATE PessoaConta SET IsPrincipal = 1 WHERE Id = @vinculoId;",
                new { vinculoId }, tx);
        }
    }
}