using Financeiro.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Financeiro.Repositorios
{
    public interface IUsuarioRepositorio
    {
        Task<int> AdicionarAsync(Usuario usuario);
        Task<Usuario?> ObterPorIdAsync(int id);
        Task<Usuario?> ObterPorEmailAsync(string emailCriptografado);
        Task<Usuario?> ObterPorNameSkipAsync(string nameSkip);
        Task<bool> NameSkipExisteAsync(string nameSkip);
        Task AtualizarUltimoAcessoAsync(int usuarioId);
        Task<IEnumerable<Usuario>> ListarAsync();
        Task AtualizarAsync(Usuario usuario);
        Task ExcluirAsync(int id);
    }
}