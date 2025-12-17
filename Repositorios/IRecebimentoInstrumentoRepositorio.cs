using Financeiro.Models.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Financeiro.Repositorios
{
    public interface IRecebimentoInstrumentoRepositorio
    {
        Task<RecebimentoViewModel?> ObterParaEdicaoAsync(int id);
        
        // Alterado para suportar paginação dentro do instrumento
        Task<(IEnumerable<RecebimentoViewModel> Itens, int Total)> ListarPaginadoPorInstrumentoAsync(int instrumentoId, int pagina, int tamanho);
        
        Task<int> InserirAsync(RecebimentoViewModel vm);
        Task AtualizarAsync(RecebimentoViewModel vm);
        Task ExcluirAsync(int id);
        Task<IEnumerable<RecebimentoViewModel>> ListarTodosAsync();
    }
}