using Financeiro.Models;
using Financeiro.Models.ViewModels; // Necessário para a listagem
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Financeiro.Repositorios
{
    public interface IUsuarioRepositorio
    {
        Task<int> AdicionarAsync(Usuario usuario);
        Task AtualizarAsync(Usuario usuario);
        Task InativarAsync(int id); // Mudado de Excluir para Inativar
        
        Task<Usuario?> ObterPorIdAsync(int id);
        Task<Usuario?> ObterPorEmailAsync(string emailCriptografado);
        Task<Usuario?> ObterPorEmailHashAsync(string emailHash);
        Task<Usuario?> ObterPorNameSkipAsync(string nameSkip);
        
        Task<bool> NameSkipExisteAsync(string nameSkip);
        Task AtualizarUltimoAcessoAsync(int usuarioId);
        
        Task<IEnumerable<Entidade>> ObterEntidadesPorUsuarioIdAsync(int usuarioId);
        Task<(IEnumerable<UsuarioListagemViewModel> Itens, int Total)> ListarPaginadoAsync(int pagina, int tamanho, bool incluirInativos);
        Task AtivarAsync(int id);
    }
}