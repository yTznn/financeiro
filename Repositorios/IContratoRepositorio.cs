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
        // Atualiza apenas os dados financeiros e de vigência do contrato pai
        Task AtualizarVigenciaEValorAsync(int contratoId, DateTime inicio, DateTime fim, decimal valorTotal);
        Task<ContratoViewModel?> ObterParaEdicaoAsync(int id);
        
        Task<(IEnumerable<ContratoListaViewModel> Itens, int TotalPaginas)> ListarPaginadoAsync(int entidadeId, int pagina, int tamanhoPagina = 10);
        
        Task<int> SugerirProximoNumeroAsync(int ano, int entidadeId); 
        
        Task<bool> VerificarUnicidadeAsync(int numero, int ano, int entidadeId, int idAtual = 0); 
        
        Task<(IEnumerable<VwFornecedor> Itens, int TotalItens)> BuscarFornecedoresPaginadoAsync(string termoBusca, int pagina, int tamanhoPagina);
        Task<IEnumerable<Natureza>> ListarTodasNaturezasAsync();
        Task<VwFornecedor?> ObterFornecedorPorIdCompletoAsync(string idCompleto);
        Task<decimal> ObterTotalComprometidoPorOrcamentoAsync(int orcamentoId, int? ignorarContratoId = null);

        // [NOVO] Adicione esta linha aqui:
        Task<IEnumerable<ContratoNatureza>> ListarNaturezasPorContratoAsync(int contratoId);
    }
}