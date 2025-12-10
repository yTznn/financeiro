using Financeiro.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Financeiro.Repositorios
{
    public interface IUsuarioRepositorio
    {
        // Alterado de Task para Task<int> para retornar o ID gerado
        Task<int> AdicionarAsync(Usuario usuario);
        
        Task<Usuario?> ObterPorIdAsync(int id);
        Task<Usuario?> ObterPorEmailAsync(string emailCriptografado);
        Task<Usuario?> ObterPorEmailHashAsync(string emailHash);
        Task<Usuario?> ObterPorNameSkipAsync(string nameSkip);
        Task<bool> NameSkipExisteAsync(string nameSkip);
        Task AtualizarUltimoAcessoAsync(int usuarioId);
        Task<IEnumerable<Usuario>> ListarAsync();
        Task AtualizarAsync(Usuario usuario);
        Task ExcluirAsync(int id);
        Task<IEnumerable<Entidade>> ObterEntidadesPorUsuarioIdAsync(int usuarioId);
    }
}