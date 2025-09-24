using System.Threading.Tasks;
using Financeiro.Models;
using Financeiro.Models.ViewModels;

namespace Financeiro.Servicos
{
    public interface IInstrumentoVersaoService
    {
        Task CriarAditivoAsync(AditivoInstrumentoViewModel vm);

        /// <summary>
        /// Cancela a última versão (aditivo) se a versão bater.
        /// Retorna a versão removida e a nova vigente (se houver).
        /// </summary>
        Task<(InstrumentoVersao removida, InstrumentoVersao? vigente)> CancelarUltimoAditivoAsync(
            int instrumentoId, int versao, string justificativa);
    }
}