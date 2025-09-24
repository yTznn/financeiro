using System.Threading.Tasks;
using Financeiro.Models;
using Financeiro.Models.ViewModels;

namespace Financeiro.Servicos
{
    /// <summary>
    /// Interface legado. Encaminha para IInstrumentoVersaoService.
    /// </summary>
    public interface IVersaoAcordoService
    {
        Task CriarAditivoAsync(AditivoInstrumentoViewModel vm);
        Task<(InstrumentoVersao removida, InstrumentoVersao? vigente)> CancelarUltimoAditivoAsync(int instrumentoId, int versao, string justificativa);
    }
}