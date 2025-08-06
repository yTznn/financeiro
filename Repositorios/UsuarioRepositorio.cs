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
                (NameSkip, EmailCriptografado, SenhaHash, NomeArquivoImagem, HashImagem, PessoaFisicaId, Ativo, DataCriacao)
                VALUES (@NameSkip, @EmailCriptografado, @SenhaHash, @NomeArquivoImagem, @HashImagem, @PessoaFisicaId, @Ativo, @DataCriacao);

                SELECT CAST(SCOPE_IDENTITY() as int);
            ";

            using var conn = _connectionFactory.CreateConnection();

            // 👇 Adiciona esta linha
            if (conn.State != ConnectionState.Open)
                conn.Open();

            return await conn.ExecuteScalarAsync<int>(sql, usuario);
        }
        public Task<Usuario?> ObterPorIdAsync(int id)
        {
            const string sql = "SELECT * FROM Usuarios WHERE Id = @Id";
            using var conn = _connectionFactory.CreateConnection();
            return conn.QueryFirstOrDefaultAsync<Usuario>(sql, new { Id = id });
        }

        public Task<Usuario?> ObterPorEmailAsync(string emailCriptografado)
        {
            const string sql = "SELECT * FROM Usuarios WHERE EmailCriptografado = @EmailCriptografado";
            using var conn = _connectionFactory.CreateConnection();
            return conn.QueryFirstOrDefaultAsync<Usuario>(sql, new { EmailCriptografado = emailCriptografado });
        }

        public Task<Usuario?> ObterPorNameSkipAsync(string nameSkip)
        {
            const string sql = "SELECT * FROM Usuarios WHERE NameSkip = @NameSkip";
            using var conn = _connectionFactory.CreateConnection();
            return conn.QueryFirstOrDefaultAsync<Usuario>(sql, new { NameSkip = nameSkip });
        }

        public async Task<bool> NameSkipExisteAsync(string nameSkip)
        {
            var sql = "SELECT COUNT(1) FROM Usuarios WHERE NameSkip = @NameSkip";
            using var conn = _connectionFactory.CreateConnection(); // ← CORRETO
            var count = await conn.QuerySingleAsync<int>(sql, new { NameSkip = nameSkip });
            return count > 0;
        }
        public Task AtualizarUltimoAcessoAsync(int usuarioId)
        {
            const string sql = "UPDATE Usuarios SET UltimoAcesso = GETDATE() WHERE Id = @UsuarioId";
            using var conn = _connectionFactory.CreateConnection();
            return conn.ExecuteAsync(sql, new { UsuarioId = usuarioId });
        }
    }
}