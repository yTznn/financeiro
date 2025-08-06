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
        Task<(IEnumerable<ContratoListaViewModel> Itens, int TotalPaginas)> ListarPaginadoAsync(int pagina, int tamanhoPagina = 10);
        Task<int> SugerirProximoNumeroAsync(int ano);
        Task<bool> VerificarUnicidadeAsync(int numero, int ano, int idAtual = 0);
        Task<(IEnumerable<VwFornecedor> Itens, bool TemMais)> BuscarFornecedoresPaginadoAsync(string termoBusca, int pagina, int tamanhoPagina = 10);
        Task<IEnumerable<Natureza>> ListarTodasNaturezasAsync();
        Task<VwFornecedor?> ObterFornecedorPorIdCompletoAsync(string idCompleto);
    }
}