using System.Collections.Generic;
using System.Threading.Tasks;
using Financeiro.Models;
using Financeiro.Models.ViewModels;

namespace Financeiro.Repositorios
{
    public interface IOrcamentoRepositorio
    {
        Task InserirAsync(OrcamentoViewModel vm);
        
        // Mantivemos a assinatura, mas a lógica interna mudou para "Upsert"
        Task AtualizarAsync(int id, OrcamentoViewModel vm);
        
        Task<Orcamento?> ObterHeaderPorIdAsync(int id);
        Task<IEnumerable<OrcamentoDetalhe>> ObterDetalhesPorOrcamentoIdAsync(int orcamentoId);
        
        // [BI] Retorna a lista paginada já com os dados de consumo preenchidos
        Task<(IEnumerable<OrcamentoListViewModel> Itens, int TotalItens)> ListarPaginadoAsync(int entidadeId, int pagina, int tamanhoPagina);
        
        Task<IEnumerable<OrcamentoListViewModel>> ListarAtivosPorEntidadeAsync(int entidadeId);
        
        Task ExcluirAsync(int id);
        
        Task<decimal> ObterTotalComprometidoPorInstrumentoAsync(int instrumentoId, int? ignorarOrcamentoId = null);
        
        Task<OrcamentoDetalhe?> ObterDetalhePorIdAsync(int id);
        
        Task<IEnumerable<OrcamentoDetalhe>> ListarDetalhesParaLancamentoAsync(int orcamentoId);
    }
}