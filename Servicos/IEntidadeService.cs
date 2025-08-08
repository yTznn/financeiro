using Financeiro.Models;
using Financeiro.Models.DTO;

namespace Financeiro.Servicos
{
    public interface IEntidadeService
    {
        Task<int> CriarAsync(Entidade entidade);
        Task AtualizarAsync(Entidade entidade);
        Task ExcluirAsync(int id);
        Task<Entidade?> ObterPorIdAsync(int id);
        Task<IEnumerable<Entidade>> ListarAsync();

        // auto-fill que o front consumirá
        Task<EntidadeAutoFillDto?> AutoFillPorCnpjAsync(string cnpj);
    }
}