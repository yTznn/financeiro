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
        
        // [ALTERADO] Agora retorna paginado e filtrado por Entidade
        Task<(IEnumerable<OrcamentoListViewModel> Itens, int TotalItens)> ListarPaginadoAsync(int entidadeId, int pagina, int tamanhoPagina);
        Task<IEnumerable<OrcamentoListViewModel>> ListarAtivosPorEntidadeAsync(int entidadeId);
        
        Task ExcluirAsync(int id);
        Task<decimal> ObterTotalComprometidoPorInstrumentoAsync(int instrumentoId, int? ignorarOrcamentoId = null);

    }
}