using Dapper;
using Financeiro.Models;
using Financeiro.Infraestrutura;
using System.Data;

public class PerfilRepositorio : IPerfilRepositorio
{
    private readonly IDbConnectionFactory _connectionFactory;

    public PerfilRepositorio(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<Perfil>> ListarAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QueryAsync<Perfil>("SELECT * FROM Perfis ORDER BY Nome");
    }

    public async Task<Perfil?> ObterPorIdAsync(int id)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Perfil>(
            "SELECT * FROM Perfis WHERE Id = @Id", new { Id = id });
    }

    public async Task AdicionarAsync(Perfil perfil)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.ExecuteAsync("INSERT INTO Perfis (Nome, Ativo) VALUES (@Nome, @Ativo)", perfil);
    }

    public async Task AtualizarAsync(Perfil perfil)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.ExecuteAsync("UPDATE Perfis SET Nome = @Nome, Ativo = @Ativo WHERE Id = @Id", perfil);
    }

    public async Task ExcluirAsync(int id)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM Perfis WHERE Id = @Id", new { Id = id });
    }
}