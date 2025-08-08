using Financeiro.Models;

namespace Financeiro.Repositorios
{
    public interface IEntidadeRepositorio
    {
        Task<int> AddAsync(Entidade entidade);
        Task UpdateAsync(Entidade entidade);
        Task DeleteAsync(int id);
        Task<Entidade?> GetByIdAsync(int id);
        Task<Entidade?> GetByCnpjAsync(string cnpj);
        Task<IEnumerable<Entidade>> ListAsync();
    }
}