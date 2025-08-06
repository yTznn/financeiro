using System.Collections.Generic;
using System.Threading.Tasks;
using Financeiro.Models;
using Financeiro.Models.ViewModels;

namespace Financeiro.Repositorios
{
    public interface INaturezaRepositorio
    {
        Task InserirAsync(NaturezaViewModel vm);
        Task AtualizarAsync(int id, NaturezaViewModel vm);
        Task<Natureza?> ObterPorIdAsync(int id);
        Task<IEnumerable<Natureza>> ListarAsync();
    }
}