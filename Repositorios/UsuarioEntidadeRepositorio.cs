using Dapper;
using Financeiro.Infraestrutura;
using Financeiro.Models;

namespace Financeiro.Repositorios
{
    public class UsuarioEntidadeRepositorio : IUsuarioEntidadeRepositorio
    {
        private readonly IDbConnectionFactory _factory;
        public UsuarioEntidadeRepositorio(IDbConnectionFactory factory) => _factory = factory;

        public async Task<IEnumerable<UsuarioEntidade>> ListarPorUsuarioAsync(int usuarioId)
        {
            const string sql = "SELECT * FROM UsuarioEntidade WHERE UsuarioId = @usuarioId;";
            using var conn = _factory.CreateConnection();
            return await conn.QueryAsync<UsuarioEntidade>(sql, new { usuarioId });
        }

        public async Task InserirAsync(UsuarioEntidade vinculo)
        {
            const string sql = @"
INSERT INTO UsuarioEntidade (UsuarioId, EntidadeId, Ativo)
VALUES (@UsuarioId, @EntidadeId, @Ativo);";
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(sql, vinculo);
        }

        /// <summary>
        /// Marca a entidade indicada como ativa e zera as demais para o usuário.
        /// </summary>
        public async Task AtualizarAtivoAsync(int usuarioId, int entidadeIdAtiva)
        {
            const string sql = @"
UPDATE UsuarioEntidade
SET Ativo = CASE WHEN EntidadeId = @entidadeIdAtiva THEN 1 ELSE 0 END
WHERE UsuarioId = @usuarioId;";
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(sql, new { usuarioId, entidadeIdAtiva });
        }

        /// <summary>
        /// Remove todos os vínculos de um usuário (útil ao regravar lista).
        /// </summary>
        public async Task RemoverTodosPorUsuarioAsync(int usuarioId)
        {
            const string sql = "DELETE FROM UsuarioEntidade WHERE UsuarioId = @usuarioId;";
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(sql, new { usuarioId });
        }
    }
}