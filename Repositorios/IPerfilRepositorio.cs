using Financeiro.Models;

public interface IPerfilRepositorio
{
    Task<IEnumerable<Perfil>> ListarAsync();
    Task<Perfil?> ObterPorIdAsync(int id);
    Task AdicionarAsync(Perfil perfil);
    Task AtualizarAsync(Perfil perfil);
    Task ExcluirAsync(int id);
}