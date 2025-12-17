using Financeiro.Models.ViewModels;
using System.Threading.Tasks;

namespace Financeiro.Repositorios
{
    public interface IRelatorioRepositorio
    {
        Task<RelatorioInstrumentoViewModel> GerarLancamentosXRecebimentosAsync(
            int instrumentoId, 
            DateTime dataInicio, 
            DateTime dataFim);
    }
}