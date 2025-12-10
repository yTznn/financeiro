using System.Collections.Generic;
using System.Data;
using System.Linq; 
using System.Threading.Tasks;
using Dapper;
using Financeiro.Infraestrutura;
using Financeiro.Models;
using Financeiro.Models.Dto; // <--- Importando a pasta correta

namespace Financeiro.Repositorios
{
    public class PermissaoRepositorio : IPermissaoRepositorio
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public PermissaoRepositorio(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<HashSet<string>> ObterPermissoesDoUsuarioAsync(int usuarioId)
        {
            using var conn = _connectionFactory.CreateConnection();

            const string sql = @"
                SELECT p.Chave
                  FROM dbo.UsuarioPermissoes up
                  JOIN dbo.Permissoes p ON up.PermissaoId = p.Id
                 WHERE up.UsuarioId = @usuarioId 
                   AND up.Concedido = 1

                UNION

                SELECT p.Chave
                  FROM dbo.PerfilPermissoes pp
                  JOIN dbo.Permissoes p ON pp.PermissaoId = p.Id
                  JOIN dbo.Usuarios u ON u.PerfilId = pp.PerfilId
                 WHERE u.Id = @usuarioId";

            var chaves = await conn.QueryAsync<string>(sql, new { usuarioId });

            return new HashSet<string>(chaves);
        }

        public async Task<IEnumerable<Permissao>> ListarTodasAsync()
        {
            using var conn = _connectionFactory.CreateConnection();
            return await conn.QueryAsync<Permissao>("SELECT * FROM dbo.Permissoes ORDER BY Modulo, Nome");
        }

        public async Task<IEnumerable<PermissaoStatusDto>> ObterStatusPermissoesUsuarioAsync(int usuarioId)
        {
            using var conn = _connectionFactory.CreateConnection();

            // Query otimizada para trazer o status (Usuario vs Perfil)
            const string sql = @"
                SELECT 
                    p.Id, 
                    p.Nome, 
                    p.Chave, 
                    p.Modulo, 
                    p.Descricao,
                    CAST(CASE WHEN up.Id IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS TemPeloUsuario,
                    CAST(CASE WHEN pp.Id IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS TemPeloPerfil
                FROM dbo.Permissoes p
                LEFT JOIN dbo.UsuarioPermissoes up 
                       ON p.Id = up.PermissaoId AND up.UsuarioId = @usuarioId
                LEFT JOIN dbo.Usuarios u 
                       ON u.Id = @usuarioId
                LEFT JOIN dbo.PerfilPermissoes pp 
                       ON p.Id = pp.PermissaoId AND pp.PerfilId = u.PerfilId
                ORDER BY p.Modulo, p.Nome";

            return await conn.QueryAsync<PermissaoStatusDto>(sql, new { usuarioId });
        }

        public async Task AtualizarPermissoesUsuarioAsync(int usuarioId, List<int> permissoesIds)
        {
            using var conn = _connectionFactory.CreateConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                // 1. Remove as permissões manuais anteriores
                const string sqlDelete = "DELETE FROM dbo.UsuarioPermissoes WHERE UsuarioId = @usuarioId";
                await conn.ExecuteAsync(sqlDelete, new { usuarioId }, tx);

                // 2. Insere as novas, se houver
                if (permissoesIds != null && permissoesIds.Any())
                {
                    const string sqlInsert = @"
                        INSERT INTO dbo.UsuarioPermissoes (UsuarioId, PermissaoId, Concedido)
                        VALUES (@UsuarioId, @PermissaoId, 1)";

                    var listaParaInserir = permissoesIds.Select(id => new { UsuarioId = usuarioId, PermissaoId = id });
                    
                    await conn.ExecuteAsync(sqlInsert, listaParaInserir, tx);
                }

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }
    }
}