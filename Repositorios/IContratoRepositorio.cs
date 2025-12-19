using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Financeiro.Models;
using Financeiro.Models.ViewModels;

namespace Financeiro.Repositorios
{
    public interface IContratoRepositorio
    {
        // CRUD Principal
        Task InserirAsync(ContratoViewModel vm);
        Task AtualizarAsync(ContratoViewModel vm);
        Task ExcluirAsync(int id);
        Task AtualizarVigenciaEValorAsync(int contratoId, DateTime inicio, DateTime fim, decimal valorTotal);
        Task<ContratoViewModel?> ObterParaEdicaoAsync(int id);

        // Listagens e Buscas
        Task<(IEnumerable<ContratoListaViewModel> Itens, int TotalPaginas)> ListarPaginadoAsync(int entidadeId, int pagina, int tamanhoPagina = 10);
        Task<(IEnumerable<VwFornecedor> Itens, int TotalItens)> BuscarFornecedoresPaginadoAsync(string termoBusca, int pagina, int tamanhoPagina);
        Task<VwFornecedor?> ObterFornecedorPorIdCompletoAsync(string idCompleto);

        // Utilitários de Regra de Negócio
        Task<int> SugerirProximoNumeroAsync(int ano, int entidadeId);
        Task<bool> VerificarUnicidadeAsync(int numero, int ano, int entidadeId, int idAtual = 0);

        // [CRÍTICO] Validação Financeira (Nova lógica da Mariazinha)
        Task<decimal> ObterTotalComprometidoPorDetalheAsync(int orcamentoDetalheId, int? ignorarContratoId = null);

        // Suporte a Rateio/Lançamento
        Task<IEnumerable<dynamic>> ListarAtivosPorFornecedorAsync(int entidadeId, int fornecedorId, string tipoPessoa);
        
        // [NOVO] Substitui as listagens de natureza antigas
        Task<IEnumerable<dynamic>> ListarItensPorContratoAsync(int contratoId);
        // Adicione isto de volta na Interface
        Task<decimal> ObterTotalComprometidoPorOrcamentoAsync(int orcamentoId, int? ignorarContratoId = null);
    }
}