using System.Threading.Tasks;
using Financeiro.Models;
using Financeiro.Models.ViewModels;

namespace Financeiro.Servicos
{
    /// <summary>
    /// Implementação legado que encaminha para IInstrumentoVersaoService.
    /// </summary>
    public class VersaoAcordoService : IVersaoAcordoService
    {
        private readonly IInstrumentoVersaoService _inner;

        public VersaoAcordoService(IInstrumentoVersaoService inner)
        {
            _inner = inner;
        }

        public Task CriarAditivoAsync(AditivoInstrumentoViewModel vm)
            => _inner.CriarAditivoAsync(vm);

        public Task<(InstrumentoVersao removida, InstrumentoVersao? vigente)> CancelarUltimoAditivoAsync(int instrumentoId, int versao, string justificativa)
            => _inner.CancelarUltimoAditivoAsync(instrumentoId, versao, justificativa);
    }
}