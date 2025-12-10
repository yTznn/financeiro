using Financeiro.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Financeiro.Repositorios
{
    public interface IEntidadeRepositorio
    {
        Task<int> AddAsync(Entidade entidade);
        Task UpdateAsync(Entidade entidade);
        Task DeleteAsync(int id);
        Task<Entidade?> GetByIdAsync(int id);
        Task<Entidade?> GetByCnpjAsync(string cnpj);
        
        // Mantém o ListarAsync original (para combos, etc)
        Task<IEnumerable<Entidade>> ListAsync();

        // NOVO: Paginação
        Task<(IEnumerable<Entidade> Itens, int TotalItens)> ListarPaginadoAsync(int pagina, int tamanhoPagina);
    }
}