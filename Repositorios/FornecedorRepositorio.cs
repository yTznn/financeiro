using Dapper;
using Financeiro.Infraestrutura;
using Financeiro.Models; // Necessário para reconhecer 'ContaBancaria'
using Financeiro.Models.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Financeiro.Repositorios
{
    public class FornecedorRepositorio : IFornecedorRepositorio
    {
        private readonly IDbConnectionFactory _factory;

        public FornecedorRepositorio(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<(IEnumerable<FornecedorViewModel> Itens, int TotalItens)> BuscarTodosPaginadoAsync(string busca, int pagina, int tamanhoPagina)
        {
            using var conn = _factory.CreateConnection();
            var term = $"%{busca}%";

            // 1. Contagem Total
            const string sqlCount = @"
                SELECT COUNT(*) FROM (
                    SELECT Id FROM PessoaFisica WHERE (@busca = '' OR Nome LIKE @term OR Cpf LIKE @term)
                    UNION ALL
                    SELECT Id FROM PessoaJuridica WHERE (@busca = '' OR RazaoSocial LIKE @term OR NomeFantasia LIKE @term OR NumeroInscricao LIKE @term)
                ) Total";

            var total = await conn.ExecuteScalarAsync<int>(sqlCount, new { busca, term });

            // 2. Busca Paginada
            const string sql = @"
                SELECT * FROM (
                    SELECT 
                        Id, 
                        Nome AS Nome, 
                        Cpf AS Documento, 
                        'PF' AS TipoPessoa,
                        Email,
                        Telefone
                    FROM PessoaFisica 
                    WHERE (@busca = '' OR Nome LIKE @term OR Cpf LIKE @term)
                    
                    UNION ALL
                    
                    SELECT 
                        Id, 
                        RazaoSocial AS Nome, 
                        NumeroInscricao AS Documento, 
                        'PJ' AS TipoPessoa,
                        Email,
                        Telefone
                    FROM PessoaJuridica 
                    WHERE (@busca = '' OR RazaoSocial LIKE @term OR NomeFantasia LIKE @term OR NumeroInscricao LIKE @term)
                ) Uniao
                ORDER BY Nome
                OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY";

            var skip = (pagina - 1) * tamanhoPagina;
            var itens = await conn.QueryAsync<FornecedorViewModel>(sql, new { busca, term, skip, take = tamanhoPagina });

            return (itens ?? Enumerable.Empty<FornecedorViewModel>(), total);
        }

        public async Task<IEnumerable<dynamic>> ListarTodosParaComboAsync()
        {
            using var conn = _factory.CreateConnection();
            
            const string sql = @"
                SELECT 
                    FornecedorId, 
                    Nome, 
                    Documento, 
                    Tipo, 
                    CONCAT(Tipo, '-', FornecedorId) AS IdCompleto 
                FROM Vw_Fornecedores 
                WHERE SituacaoAtiva = 1
                ORDER BY Nome";

            return await conn.QueryAsync(sql);
        }

        // [NOVO MÉTODO IMPLEMENTADO]
        // Corrige o erro CS0535 ao implementar o contrato da Interface
        public async Task<ContaBancaria?> ObterContaPrincipalAsync(int fornecedorId, string tipoPessoa)
        {
            using var conn = _factory.CreateConnection();
            
            // Busca a conta marcada como Principal (IsPrincipal = 1) na tabela de junção
            string sql = @"
                SELECT cb.* FROM ContaBancaria cb
                INNER JOIN PessoaConta pc ON pc.ContaBancariaId = cb.Id
                WHERE pc.IsPrincipal = 1 ";

            // Filtra pela tabela correta dependendo do tipo
            if (tipoPessoa == "PF")
                sql += "AND pc.PessoaFisicaId = @fornecedorId";
            else
                sql += "AND pc.PessoaJuridicaId = @fornecedorId";

            return await conn.QuerySingleOrDefaultAsync<ContaBancaria>(sql, new { fornecedorId });
        }
    }
}