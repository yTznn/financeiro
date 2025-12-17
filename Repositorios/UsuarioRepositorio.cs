using Dapper;
using Financeiro.Infraestrutura;
using Financeiro.Models;
using Financeiro.Models.ViewModels; // Importante para a ViewModel da listagem
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Financeiro.Repositorios
{
    public class UsuarioRepositorio : IUsuarioRepositorio
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public UsuarioRepositorio(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<int> AdicionarAsync(Usuario usuario)
        {
            const string sql = @"
                INSERT INTO Usuarios
                (NameSkip, EmailCriptografado, EmailHash, SenhaHash, NomeArquivoImagem, HashImagem, PessoaFisicaId, PerfilId, Ativo, DataCriacao)
                VALUES 
                (@NameSkip, @EmailCriptografado, @EmailHash, @SenhaHash, @NomeArquivoImagem, @HashImagem, @PessoaFisicaId, @PerfilId, @Ativo, @DataCriacao);
                SELECT CAST(SCOPE_IDENTITY() as int);";

            using var conn = _connectionFactory.CreateConnection();
            return await conn.QuerySingleAsync<int>(sql, usuario);
        }

        public async Task AtualizarAsync(Usuario usuario)
        {
            const string sql = @"
                UPDATE Usuarios
                SET NameSkip = @NameSkip,
                    EmailCriptografado = @EmailCriptografado,
                    EmailHash = @EmailHash,
                    SenhaHash = ISNULL(@SenhaHash, SenhaHash),
                    NomeArquivoImagem = ISNULL(@NomeArquivoImagem, NomeArquivoImagem),
                    HashImagem = ISNULL(@HashImagem, HashImagem),
                    PessoaFisicaId = @PessoaFisicaId,
                    PerfilId = @PerfilId,
                    Ativo = @Ativo
                WHERE Id = @Id;";

            using var conn = _connectionFactory.CreateConnection();
            await conn.ExecuteAsync(sql, usuario);
        }

        public async Task InativarAsync(int id)
        {
            const string sql = "UPDATE Usuarios SET Ativo = 0 WHERE Id = @Id";
            using var conn = _connectionFactory.CreateConnection();
            await conn.ExecuteAsync(sql, new { Id = id });
        }

        public async Task<Usuario?> ObterPorIdAsync(int id)
        {
            const string sql = "SELECT * FROM Usuarios WHERE Id = @Id";
            using var conn = _connectionFactory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<Usuario>(sql, new { Id = id });
        }

        public async Task<Usuario?> ObterPorEmailAsync(string emailCriptografado)
        {
            const string sql = "SELECT * FROM Usuarios WHERE EmailCriptografado = @EmailCriptografado";
            using var conn = _connectionFactory.CreateConnection();
            
            // CORREÇÃO: Atribuição explícita { PropriedadeBanco = VariavelMetodo }
            return await conn.QueryFirstOrDefaultAsync<Usuario>(
                new CommandDefinition(sql, new { EmailCriptografado = emailCriptografado }, commandTimeout: 10));
        }

        public async Task<Usuario?> ObterPorEmailHashAsync(string emailHash)
        {
            const string sql = "SELECT * FROM Usuarios WHERE EmailHash = @EmailHash";
            using var conn = _connectionFactory.CreateConnection();
            
            // CORREÇÃO: Atribuição explícita
            return await conn.QueryFirstOrDefaultAsync<Usuario>(sql, new { EmailHash = emailHash });
        }

        public async Task<Usuario?> ObterPorNameSkipAsync(string nameSkip)
        {
            const string sql = "SELECT * FROM Usuarios WHERE NameSkip = @NameSkip";
            using var conn = _connectionFactory.CreateConnection();
            
            // CORREÇÃO: Atribuição explícita
            return await conn.QueryFirstOrDefaultAsync<Usuario>(
                new CommandDefinition(sql, new { NameSkip = nameSkip }, commandTimeout: 10));
        }

        public async Task<bool> NameSkipExisteAsync(string nameSkip)
        {
            const string sql = "SELECT COUNT(1) FROM Usuarios WHERE NameSkip = @NameSkip";
            using var conn = _connectionFactory.CreateConnection();
            
            // CORREÇÃO: Atribuição explícita
            return await conn.QuerySingleAsync<int>(sql, new { NameSkip = nameSkip }) > 0;
        }

        public async Task AtualizarUltimoAcessoAsync(int usuarioId)
        {
            const string sql = "UPDATE Usuarios SET UltimoAcesso = GETDATE() WHERE Id = @UsuarioId";
            using var conn = _connectionFactory.CreateConnection();
            await conn.ExecuteAsync(sql, new { UsuarioId = usuarioId });
        }

        public async Task<IEnumerable<Entidade>> ObterEntidadesPorUsuarioIdAsync(int usuarioId)
        {
            const string sql = @"
                SELECT e.Id, e.Sigla
                FROM Entidade e
                INNER JOIN UsuarioEntidade ue ON ue.EntidadeId = e.Id
                WHERE ue.UsuarioId = @UsuarioId AND e.Ativo = 1";

            using var conn = _connectionFactory.CreateConnection();
            return await conn.QueryAsync<Entidade>(sql, new { UsuarioId = usuarioId });
        }

        // --- MÉTODO NOVO PARA LISTAGEM PREMIUM ---
        public async Task<(IEnumerable<UsuarioListagemViewModel> Itens, int Total)> ListarPaginadoAsync(int pagina, int tamanho, bool incluirInativos)
        {
            var p = pagina < 1 ? 1 : pagina;
            var offset = (p - 1) * tamanho;

            // Filtro Dinâmico
            string filtroAtivo = incluirInativos ? "1=1" : "u.Ativo = 1";

            var sql = $@"
                SELECT 
                    u.Id, u.NameSkip, u.EmailCriptografado as Email, -- Alias para casar com a ViewModel
                    u.Ativo, u.HashImagem, u.UltimoAcesso,
                    CONCAT(pf.Nome, ' ', pf.Sobrenome) AS NomePessoaFisica,
                    p.Nome AS NomePerfil
                FROM Usuarios u
                LEFT JOIN PessoaFisica pf ON u.PessoaFisicaId = pf.Id
                LEFT JOIN Perfis p ON u.PerfilId = p.Id
                WHERE {filtroAtivo}
                ORDER BY u.NameSkip
                OFFSET @Offset ROWS FETCH NEXT @Tamanho ROWS ONLY;

                SELECT COUNT(*) FROM Usuarios u WHERE {filtroAtivo};";

            using var conn = _connectionFactory.CreateConnection();
            using var multi = await conn.QueryMultipleAsync(sql, new { Offset = offset, Tamanho = tamanho });

            var itens = await multi.ReadAsync<UsuarioListagemViewModel>();
            var total = await multi.ReadFirstAsync<int>();

            return (itens, total);
        }
        // Adicione este método na classe UsuarioRepositorio
        public async Task AtivarAsync(int id)
        {
            const string sql = "UPDATE Usuarios SET Ativo = 1 WHERE Id = @Id";
            using var conn = _connectionFactory.CreateConnection();
            await conn.ExecuteAsync(sql, new { Id = id });
        }
    }
}