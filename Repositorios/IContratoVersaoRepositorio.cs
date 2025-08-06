using System.Collections.Generic;
using System.Threading.Tasks;
using Financeiro.Models;

namespace Financeiro.Repositorios
{
    /// <summary>
    /// Define as operações de banco de dados para o histórico de versões de Contrato.
    /// </summary>
    public interface IContratoVersaoRepositorio
    {
        /// <summary>
        /// Insere um novo registro de versão de contrato (original ou aditivo).
        /// </summary>
        Task InserirAsync(ContratoVersao versao);

        /// <summary>
        /// Obtém a versão mais recente de um contrato específico.
        /// </summary>
        Task<ContratoVersao?> ObterVersaoAtualAsync(int contratoId);

        /// <summary>
        /// Lista todas as versões de um contrato, ordenadas da mais recente para a mais antiga.
        /// </summary>
        Task<IEnumerable<ContratoVersao>> ListarPorContratoAsync(int contratoId);
        
        /// <summary>
        /// Lista o histórico de versões de um contrato de forma paginada.
        /// </summary>
        Task<(IEnumerable<ContratoVersao> Itens, int TotalPaginas)> ListarPaginadoAsync(int contratoId, int pagina, int tamanhoPagina = 5);
    }
}