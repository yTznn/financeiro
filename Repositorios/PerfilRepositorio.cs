using Dapper;
using Financeiro.Infraestrutura;
using Financeiro.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Financeiro.Repositorios
{
    public class PerfilRepositorio : IPerfilRepositorio
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public PerfilRepositorio(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task InserirAsync(Perfil perfil)
        {
            const string sql = @"
                INSERT INTO Perfis (Nome, Ativo) 
                VALUES (@Nome, @Ativo)";
            
            using var conn = _connectionFactory.CreateConnection();
            await conn.ExecuteAsync(sql, perfil);
        }

        public async Task AtualizarAsync(Perfil perfil)
        {
            const string sql = @"
                UPDATE Perfis 
                SET Nome = @Nome, Ativo = @Ativo 
                WHERE Id = @Id";
            
            using var conn = _connectionFactory.CreateConnection();
            await conn.ExecuteAsync(sql, perfil);
        }

        public async Task InativarAsync(int id)
        {
            // Exclusão Lógica: Apenas desativa o registro
            const string sql = "UPDATE Perfis SET Ativo = 0 WHERE Id = @Id";
            
            using var conn = _connectionFactory.CreateConnection();
            await conn.ExecuteAsync(sql, new { Id = id });
        }

        public async Task<Perfil?> ObterPorIdAsync(int id)
        {
            const string sql = "SELECT * FROM Perfis WHERE Id = @Id";
            
            using var conn = _connectionFactory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<Perfil>(sql, new { Id = id });
        }

        public async Task<(IEnumerable<Perfil> Itens, int Total)> ListarPaginadoAsync(int pagina, int tamanho)
        {
            var p = pagina < 1 ? 1 : pagina;
            var offset = (p - 1) * tamanho;

            // Busca apenas os perfis ATIVOS para a listagem
            const string sql = @"
                SELECT * FROM Perfis 
                WHERE Ativo = 1 
                ORDER BY Nome 
                OFFSET @Offset ROWS FETCH NEXT @Tamanho ROWS ONLY;
                
                SELECT COUNT(*) FROM Perfis WHERE Ativo = 1;";

            using var conn = _connectionFactory.CreateConnection();
            using var multi = await conn.QueryMultipleAsync(sql, new { Offset = offset, Tamanho = tamanho });

            var itens = await multi.ReadAsync<Perfil>();
            var total = await multi.ReadFirstAsync<int>();

            return (itens, total);
        }
        public async Task<IEnumerable<Perfil>> ListarTodosAsync()
        {
            const string sql = "SELECT * FROM Perfis WHERE Ativo = 1 ORDER BY Nome";
            
            using var conn = _connectionFactory.CreateConnection();
            return await conn.QueryAsync<Perfil>(sql);
        }
    }
}