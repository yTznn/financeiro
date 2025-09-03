using System.Threading.Tasks;
using Financeiro.Models;
using Financeiro.Models.ViewModels;

namespace Financeiro.Servicos
{
    public interface IInstrumentoVersaoService
    {
        Task CriarAditivoAsync(AditivoViewModel vm);

        Task<(AcordoVersao removida, AcordoVersao vigente)> CancelarUltimoAditivoAsync(
            int instrumentoId,
            int versaoEsperada,
            string justificativa);
    }
}