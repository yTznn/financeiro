using Financeiro.Models;
using Financeiro.Models.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Financeiro.Repositorios
{
    public interface IFornecedorRepositorio
    {
        // Seus métodos existentes...
        Task<(IEnumerable<FornecedorViewModel> Itens, int TotalItens)> BuscarTodosPaginadoAsync(string busca, int pagina, int tamanhoPagina);
        Task<IEnumerable<dynamic>> ListarTodosParaComboAsync();

        // [ADICIONADO AGORA]
        Task<ContaBancaria?> ObterContaPrincipalAsync(int fornecedorId, string tipoPessoa);
    }
}