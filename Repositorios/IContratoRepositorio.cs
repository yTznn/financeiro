using System.Collections.Generic;
using System.Threading.Tasks;
using Financeiro.Models;
using Financeiro.Models.ViewModels;

namespace Financeiro.Repositorios
{
    /// <summary>
    /// Define as operações de banco de dados para a entidade Contrato.
    /// </summary>
    public interface IContratoRepositorio
    {
        // --- Operações de Gravação (CRUD) ---

        /// <summary>
        /// Insere um novo contrato e seus vínculos com naturezas no banco de dados.
        /// </summary>
        Task InserirAsync(ContratoViewModel vm);

        /// <summary>
        /// Atualiza um contrato existente e seus vínculos com naturezas.
        /// </summary>
        Task AtualizarAsync(ContratoViewModel vm);

        /// <summary>
        /// Exclui um contrato do banco de dados.
        /// </summary>
        Task ExcluirAsync(int id);


        // --- Operações de Consulta ---

        /// <summary>
        /// Obtém um único contrato pelo seu ID, incluindo suas naturezas vinculadas.
        /// </summary>
        Task<ContratoViewModel?> ObterParaEdicaoAsync(int id);

        /// <summary>
        /// Lista os contratos de forma paginada para a tela principal.
        /// </summary>
        Task<(IEnumerable<Contrato> Itens, int TotalPaginas)> ListarPaginadoAsync(int pagina, int tamanhoPagina = 10);


        // --- Métodos de Negócio e Validação ---

        /// <summary>
        /// Sugere o próximo número de contrato disponível para um determinado ano.
        /// </summary>
        Task<int> SugerirProximoNumeroAsync(int ano);

        /// <summary>
        /// Verifica se já existe um contrato ativo com o mesmo número e ano.
        /// </summary>
        Task<bool> VerificarUnicidadeAsync(int numero, int ano, int idAtual = 0);


        // --- Métodos de Busca para Formulário (Dropdowns, etc.) ---

        /// <summary>
        /// Busca fornecedores (PF e PJ) de forma paginada para o campo de busca.
        /// </summary>
        Task<(IEnumerable<VwFornecedor> Itens, bool TemMais)> BuscarFornecedoresPaginadoAsync(string termoBusca, int pagina, int tamanhoPagina = 10);

        /// <summary>
        /// Lista todas as naturezas cadastradas.
        /// </summary>
        Task<IEnumerable<Natureza>> ListarTodasNaturezasAsync();
        Task<VwFornecedor?> ObterFornecedorPorIdCompletoAsync(string idCompleto);

    }
}