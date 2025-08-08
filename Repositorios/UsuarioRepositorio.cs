using Dapper;
using Financeiro.Models;
using Financeiro.Infraestrutura;
using System.Data;

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
                VALUES (@NameSkip, @EmailCriptografado, @EmailHash, @SenhaHash, @NomeArquivoImagem, @HashImagem, @PessoaFisicaId, @PerfilId, @Ativo, @DataCriacao);

                SELECT CAST(SCOPE_IDENTITY() as int);
            ";

            using var conn = _connectionFactory.CreateConnection();
            if (conn.State != ConnectionState.Open)
                conn.Open();

            return await conn.ExecuteScalarAsync<int>(sql, usuario);
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
            return await conn.QueryFirstOrDefaultAsync<Usuario>(
                new CommandDefinition(sql, new { EmailCriptografado = emailCriptografado }, commandTimeout: 10));
        }

        public async Task<Usuario?> ObterPorEmailHashAsync(string emailHash)
        {
            const string sql = "SELECT * FROM Usuarios WHERE EmailHash = @EmailHash";
            using var conn = _connectionFactory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<Usuario>(sql, new { EmailHash = emailHash });
        }

        public async Task<Usuario?> ObterPorNameSkipAsync(string nameSkip)
        {
            const string sql = "SELECT * FROM Usuarios WHERE NameSkip = @NameSkip";
            using var conn = _connectionFactory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<Usuario>(
                new CommandDefinition(sql, new { NameSkip = nameSkip }, commandTimeout: 10));
        }

        public async Task<bool> NameSkipExisteAsync(string nameSkip)
        {
            const string sql = "SELECT COUNT(1) FROM Usuarios WHERE NameSkip = @NameSkip";
            using var conn = _connectionFactory.CreateConnection();
            return await conn.QuerySingleAsync<int>(sql, new { NameSkip = nameSkip }) > 0;
        }

        public async Task AtualizarUltimoAcessoAsync(int usuarioId)
        {
            const string sql = "UPDATE Usuarios SET UltimoAcesso = GETDATE() WHERE Id = @UsuarioId";
            using var conn = _connectionFactory.CreateConnection();
            if (conn.State != ConnectionState.Open)
                conn.Open();

            await conn.ExecuteAsync(sql, new { UsuarioId = usuarioId });
        }

        public async Task<IEnumerable<Usuario>> ListarAsync()
        {
            const string sql = "SELECT * FROM Usuarios ORDER BY DataCriacao DESC";
            using var conn = _connectionFactory.CreateConnection();
            return await conn.QueryAsync<Usuario>(sql);
        }

        public async Task AtualizarAsync(Usuario usuario)
        {
            const string sql = @"
                UPDATE Usuarios
                SET
                    NameSkip = @NameSkip,
                    EmailCriptografado = @EmailCriptografado,
                    EmailHash = @EmailHash,
                    SenhaHash = ISNULL(@SenhaHash, SenhaHash),
                    NomeArquivoImagem = ISNULL(@NomeArquivoImagem, NomeArquivoImagem),
                    HashImagem = ISNULL(@HashImagem, HashImagem),
                    PessoaFisicaId = @PessoaFisicaId,
                    PerfilId = @PerfilId
                WHERE Id = @Id;
            ";

            using var conn = _connectionFactory.CreateConnection();
            if (conn.State != ConnectionState.Open)
                conn.Open();

            await conn.ExecuteAsync(sql, usuario);
        }

        public async Task ExcluirAsync(int id)
        {
            const string sql = "DELETE FROM Usuarios WHERE Id = @Id";
            using var conn = _connectionFactory.CreateConnection();
            if (conn.State != ConnectionState.Open)
                conn.Open();

            await conn.ExecuteAsync(sql, new { Id = id });
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
    }
}