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
        Task<IEnumerable<OrcamentoListViewModel>> ListarAsync();
        Task ExcluirAsync(int id);

        // [NOVO] Método para somar quanto já foi gasto do instrumento
        Task<decimal> ObterTotalComprometidoPorInstrumentoAsync(int instrumentoId, int? ignorarOrcamentoId = null);
    }
}