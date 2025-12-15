using System.Collections.Generic;
using System.Threading.Tasks;
using Financeiro.Models.ViewModels;

namespace Financeiro.Repositorios
{
    public interface IFornecedorRepositorio
    {
        // O método unificado e paginado que antes íamos colocar em Contratos
        Task<(IEnumerable<FornecedorViewModel> Itens, int TotalItens)> BuscarTodosPaginadoAsync(string busca, int pagina, int tamanhoPagina);
        // Adicione este método
        Task<IEnumerable<dynamic>> ListarTodosParaComboAsync();
    }
}