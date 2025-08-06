using System.Collections.Generic;
using System.Threading.Tasks;
using Financeiro.Models;
using Financeiro.Models.ViewModels;

namespace Financeiro.Repositorios
{
    public interface IOrcamentoRepositorio
    {
        Task InserirAsync(OrcamentoViewModel vm);
        Task AtualizarAsync(int id, OrcamentoViewModel vm);
        Task<Orcamento?> ObterHeaderPorIdAsync(int id);
        Task<IEnumerable<OrcamentoDetalhe>> ObterDetalhesPorOrcamentoIdAsync(int orcamentoId);
        Task<IEnumerable<Orcamento>> ListarAsync();
        Task ExcluirAsync(int id);
    }
}