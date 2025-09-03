using System.Collections.Generic;
using System.Threading.Tasks;
using Financeiro.Models;
using Financeiro.Models.ViewModels;

namespace Financeiro.Repositorios
{
    public interface IInstrumentoRepositorio
    {
        Task InserirAsync(TipoAcordoViewModel vm);
        Task AtualizarAsync(int id, TipoAcordoViewModel vm);
        Task<TipoAcordo?> ObterPorIdAsync(int id);
        Task<IEnumerable<TipoAcordo>> ListarAsync();
        Task ExcluirAsync(int id);

        Task<bool> ExisteNumeroAsync(string numero, int? ignorarId = null);
        Task<string> SugerirProximoNumeroAsync(int ano);
    }
}