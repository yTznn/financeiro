using Financeiro.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Financeiro.Repositorios
{
    public interface IPerfilRepositorio
    {
        Task InserirAsync(Perfil perfil);
        Task AtualizarAsync(Perfil perfil);
        Task InativarAsync(int id);
        Task<Perfil?> ObterPorIdAsync(int id);
        Task<(IEnumerable<Perfil> Itens, int Total)> ListarPaginadoAsync(int pagina, int tamanho);
        Task<IEnumerable<Perfil>> ListarTodosAsync();
    }
}