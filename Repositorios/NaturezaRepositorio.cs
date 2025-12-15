using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Financeiro.Infraestrutura;
using Financeiro.Models;
using Financeiro.Models.ViewModels;

namespace Financeiro.Repositorios
{
    public class NaturezaRepositorio : INaturezaRepositorio
    {
        private readonly IDbConnectionFactory _factory;

        public NaturezaRepositorio(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task InserirAsync(NaturezaViewModel vm)
        {
            const string sql = @"
                INSERT INTO dbo.Natureza (Nome, NaturezaMedica, Ativo)
                VALUES (@Nome, @NaturezaMedica, @Ativo);";

            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(sql, vm);
        }

        public async Task AtualizarAsync(int id, NaturezaViewModel vm)
        {
            const string sql = @"
                UPDATE dbo.Natureza
                SET    Nome           = @Nome,
                       NaturezaMedica = @NaturezaMedica,
                       Ativo          = @Ativo
                WHERE  Id = @Id;";

            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(sql, new
            {
                vm.Nome,
                vm.NaturezaMedica,
                vm.Ativo,
                Id = id
            });
        }

        public async Task<Natureza?> ObterPorIdAsync(int id)
        {
            const string sql = "SELECT * FROM dbo.Natureza WHERE Id = @id;";
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<Natureza>(sql, new { id });
        }

        // --- MÉTODO ALTERADO PARA PERFORMANCE ---
        public async Task<IEnumerable<Natureza>> ListarAsync(int? limite = null)
        {
            // Se tiver limite, usa TOP, senão traz tudo (padrão antigo)
            string sql = limite.HasValue 
                ? "SELECT TOP (@Limit) * FROM dbo.Natureza ORDER BY Nome;" 
                : "SELECT * FROM dbo.Natureza ORDER BY Nome;";

            using var conn = _factory.CreateConnection();
            return await conn.QueryAsync<Natureza>(sql, new { Limit = limite });
        }

        public async Task<IEnumerable<Natureza>> ListarTodasAsync()
        {
            const string sql = "SELECT * FROM Natureza WHERE Ativo = 1 ORDER BY Nome";
            using var conn = _factory.CreateConnection();
            return await conn.QueryAsync<Natureza>(sql);
        }
        public async Task<(IEnumerable<Natureza> Itens, int Total)> ListarPaginadoAsync(int pagina, int tamanho)
        {
            var p = pagina < 1 ? 1 : pagina;
            var offset = (p - 1) * tamanho;
            
            const string sql = @"
                SELECT * FROM Natureza 
                WHERE Ativo = 1
                ORDER BY Nome 
                OFFSET @Offset ROWS FETCH NEXT @Tamanho ROWS ONLY;
                
                SELECT COUNT(*) FROM Natureza WHERE Ativo = 1;";

            using var conn = _factory.CreateConnection();
            using var multi = await conn.QueryMultipleAsync(sql, new { Offset = offset, Tamanho = tamanho });
            
            var itens = await multi.ReadAsync<Natureza>();
            var total = await multi.ReadFirstAsync<int>();
            
            return (itens, total);
        }
    }
}