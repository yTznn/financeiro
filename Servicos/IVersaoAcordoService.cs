using System.Threading.Tasks;
using Financeiro.Models.ViewModels;

namespace Financeiro.Servicos
{
    public interface IVersaoAcordoService
    {
        Task CriarAditivoAsync(AditivoViewModel vm);
    }
}