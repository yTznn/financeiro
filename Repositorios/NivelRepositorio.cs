using Dapper;
using Financeiro.Infraestrutura;
using Financeiro.Models.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Financeiro.Repositorios
{
    public class NivelRepositorio : INivelRepositorio
    {
        private readonly IDbConnectionFactory _cf;

        public NivelRepositorio(IDbConnectionFactory cf)
        {
            _cf = cf;
        }

        public async Task<IEnumerable<NivelDto>> BuscarAsync(string termo, int? nivel = null, bool incluirInativos = false)
        {
            var sql = @"
                SELECT Id, Nome, IsNivel1, IsNivel2, IsNivel3, Ativo
                FROM   Nivel
                WHERE  (@nivel IS NULL
                        OR (@nivel = 1 AND IsNivel1 = 1)
                        OR (@nivel = 2 AND IsNivel2 = 1)
                        OR (@nivel = 3 AND IsNivel3 = 1))
                   AND Nome LIKE @t";

            if (!incluirInativos)
                sql += " AND Ativo = 1";

            sql += " ORDER BY Nome";

            using var db = _cf.CreateConnection();
            return await db.QueryAsync<NivelDto>(sql, new { t = $"%{termo}%", nivel });
        }

        public async Task<NivelDto> ObterPorIdAsync(int id)
        {
            const string sql = "SELECT * FROM Nivel WHERE Id = @id";
            using var db = _cf.CreateConnection();
            return await db.QueryFirstOrDefaultAsync<NivelDto>(sql, new { id });
        }

        public async Task<bool> ExisteNomeAsync(string nome, int? idIgnorar = null)
        {
            var sql = "SELECT 1 FROM Nivel WHERE Nome = @nome";
            if (idIgnorar.HasValue) sql += " AND Id <> @idIgnorar";
            using var db = _cf.CreateConnection();
            return await db.ExecuteScalarAsync<bool>(sql, new { nome, idIgnorar });
        }

        public async Task<int> InserirAsync(NivelDto d)
        {
            const string sql = @"
                INSERT INTO Nivel (Nome, IsNivel1, IsNivel2, IsNivel3, Ativo)
                VALUES (@Nome, @IsNivel1, @IsNivel2, @IsNivel3, @Ativo);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            
            using var db = _cf.CreateConnection();
            return await db.ExecuteScalarAsync<int>(sql, d);
        }

        public async Task AtualizarAsync(NivelDto d)
        {
            const string sql = @"
                UPDATE Nivel 
                SET Nome = @Nome, 
                    IsNivel1 = @IsNivel1,
                    IsNivel2 = @IsNivel2, 
                    IsNivel3 = @IsNivel3, 
                    Ativo = @Ativo 
                WHERE Id = @Id";
            
            using var db = _cf.CreateConnection();
            await db.ExecuteAsync(sql, d);
        }

        public async Task InativarAsync(int id)
        {
            using var db = _cf.CreateConnection();
            await db.ExecuteAsync("UPDATE Nivel SET Ativo = 0 WHERE Id = @id", new { id });
        }

        public async Task<(IEnumerable<NivelDto> Itens, int Total)> ListarPaginadoAsync(int pagina, int tamanho)
        {
            var p = pagina < 1 ? 1 : pagina;
            var offset = (p - 1) * tamanho;

            const string sql = @"
                SELECT * FROM Nivel 
                WHERE Ativo = 1
                ORDER BY Nome 
                OFFSET @Offset ROWS FETCH NEXT @Tamanho ROWS ONLY;
                
                SELECT COUNT(*) FROM Nivel WHERE Ativo = 1;";

            using var conn = _cf.CreateConnection();
            using var multi = await conn.QueryMultipleAsync(sql, new { Offset = offset, Tamanho = tamanho });

            var itens = await multi.ReadAsync<NivelDto>();
            var total = await multi.ReadFirstAsync<int>();

            return (itens, total);
        }
    }
}