using System.Collections.Generic;
using System.Threading.Tasks;
using Financeiro.Models;
using Financeiro.Models.ViewModels;

namespace Financeiro.Repositorios
{
    public interface IInstrumentoRepositorio
    {
        Task InserirAsync(InstrumentoViewModel vm);
        Task AtualizarAsync(int id, InstrumentoViewModel vm);
        Task<Instrumento?> ObterPorIdAsync(int id);
        Task<IEnumerable<Instrumento>> ListarAsync();
        Task ExcluirAsync(int id);

        Task<bool> ExisteNumeroAsync(string numero, int? ignorarId = null);
        Task<string> SugerirProximoNumeroAsync(int ano);
    }
}