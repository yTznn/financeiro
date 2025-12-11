using System.Collections.Generic;
using System.Threading.Tasks;
using Financeiro.Models;
using Financeiro.Models.ViewModels;

namespace Financeiro.Repositorios
{
    public interface IContratoRepositorio
    {
        Task InserirAsync(ContratoViewModel vm);
        Task AtualizarAsync(ContratoViewModel vm);
        Task ExcluirAsync(int id);
        Task<ContratoViewModel?> ObterParaEdicaoAsync(int id);
        
        // [IMPORTANTE] Adicionado entidadeId aqui
        Task<(IEnumerable<ContratoListaViewModel> Itens, int TotalPaginas)> ListarPaginadoAsync(int entidadeId, int pagina, int tamanhoPagina = 10);
        
        Task<int> SugerirProximoNumeroAsync(int ano);
        Task<bool> VerificarUnicidadeAsync(int numero, int ano, int idAtual = 0);
        
        // Mantivemos este pois o controller de contratos usa para o autocomplete
        Task<(IEnumerable<VwFornecedor> Itens, int TotalItens)> BuscarFornecedoresPaginadoAsync(string termoBusca, int pagina, int tamanhoPagina);
        
        Task<IEnumerable<Natureza>> ListarTodasNaturezasAsync();
        Task<VwFornecedor?> ObterFornecedorPorIdCompletoAsync(string idCompleto);
        Task<decimal> ObterTotalComprometidoPorOrcamentoAsync(int orcamentoId, int? ignorarContratoId = null);
    }
}