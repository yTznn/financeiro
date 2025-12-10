using Dapper;
using Financeiro.Models;
using Financeiro.Repositorios;
using Financeiro.Infraestrutura;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Financeiro.Repositorios
{
    public class EntidadeRepositorio : IEntidadeRepositorio
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public EntidadeRepositorio(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<int> AddAsync(Entidade entidade)
        {
            const string sql = @"
                INSERT INTO Entidade
                    (Nome, Sigla, Cnpj, ContaBancariaId, EnderecoId, Ativo, VinculaUsuario, Observacao)
                VALUES
                    (@Nome, @Sigla, @Cnpj, @ContaBancariaId, @EnderecoId, @Ativo, @VinculaUsuario, @Observacao);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            using var conn = _connectionFactory.CreateConnection();
            return await conn.QuerySingleAsync<int>(sql, entidade);
        }

        public async Task UpdateAsync(Entidade entidade)
        {
            const string sql = @"
                UPDATE Entidade
                SET Nome            = @Nome,
                    Sigla           = @Sigla,
                    Cnpj            = @Cnpj,
                    ContaBancariaId = @ContaBancariaId,
                    EnderecoId      = @EnderecoId,
                    Ativo           = @Ativo,
                    VinculaUsuario  = @VinculaUsuario,
                    Observacao      = @Observacao
                WHERE Id = @Id;";

            using var conn = _connectionFactory.CreateConnection();
            await conn.ExecuteAsync(sql, entidade);
        }

        public async Task DeleteAsync(int id)
        {
            const string sql = "DELETE FROM Entidade WHERE Id = @Id;";
            using var conn = _connectionFactory.CreateConnection();
            await conn.ExecuteAsync(sql, new { Id = id });
        }

        public async Task<Entidade?> GetByIdAsync(int id)
        {
            const string sql = "SELECT * FROM Entidade WHERE Id = @Id;";
            using var conn = _connectionFactory.CreateConnection();
            return await conn.QuerySingleOrDefaultAsync<Entidade>(sql, new { Id = id });
        }

        public async Task<Entidade?> GetByCnpjAsync(string cnpj)
        {
            const string sql = "SELECT * FROM Entidade WHERE Cnpj = @Cnpj;";
            using var conn = _connectionFactory.CreateConnection();
            return await conn.QuerySingleOrDefaultAsync<Entidade>(sql, new { Cnpj = cnpj });
        }

        public async Task<IEnumerable<Entidade>> ListAsync()
        {
            const string sql = "SELECT * FROM Entidade ORDER BY Nome;";
            using var conn = _connectionFactory.CreateConnection();
            return await conn.QueryAsync<Entidade>(sql);
        }

        // Método OTIMIZADO para trazer Endereço Principal junto
        public async Task<(IEnumerable<Entidade> Itens, int TotalItens)> ListarPaginadoAsync(int pagina, int tamanhoPagina)
        {
            using var conn = _connectionFactory.CreateConnection();

            // 1. Total
            const string sqlCount = "SELECT COUNT(*) FROM Entidade";
            var total = await conn.ExecuteScalarAsync<int>(sqlCount);

            // 2. Itens da página + Endereço Principal (JOIN)
            // Usamos um CTE ou Subquery para pegar o ID do endereço principal se houver
            const string sql = @"
                SELECT 
                    e.*, 
                    end_princ.* -- Traz colunas do endereço
                FROM Entidade e
                LEFT JOIN EntidadeEndereco ee ON e.Id = ee.EntidadeId AND ee.Principal = 1
                LEFT JOIN Endereco end_princ ON ee.EnderecoId = end_princ.Id
                ORDER BY e.Nome
                OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY;";

            var skip = (pagina - 1) * tamanhoPagina;

            // Mapeamento Dapper (Entidade + Endereco)
            var itens = await conn.QueryAsync<Entidade, Endereco, Entidade>(
                sql,
                (entidade, endereco) => 
                {
                    // Se veio endereço, anexa (precisamos garantir que Entidade tenha essa propriedade 'virtual' ou 'DTO')
                    // Como seu Model 'Entidade' talvez não tenha a propriedade 'EnderecoPrincipal' carregada, 
                    // vamos usar um truque ou precisaríamos mudar o Model.
                    // PELA SIMPLICIDADE: Vamos assumir que você vai ajustar o Model no próximo passo.
                    entidade.EnderecoPrincipal = endereco; 
                    return entidade;
                },
                new { skip, take = tamanhoPagina },
                splitOn: "Id" // O Dapper quebra no Id do Endereco
            );

            return (itens, total);
        }
    }
}