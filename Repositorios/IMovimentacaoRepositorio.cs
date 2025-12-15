using Financeiro.Models;
using Financeiro.Models.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Financeiro.Repositorios
{
    public interface IMovimentacaoRepositorio
    {
        /// <summary>
        /// Insere uma movimentação e seus rateios, retornando o ID gerado.
        /// </summary>
        Task<int> InserirAsync(MovimentacaoViewModel vm);

        Task<IEnumerable<MovimentacaoFinanceira>> ListarAsync();

        /// <summary>
        /// Busca a movimentação completa (com rateios) para realizar estorno.
        /// </summary>
        Task<MovimentacaoViewModel?> ObterCompletoPorIdAsync(int id);
        Task ExcluirAsync(int id);
    }
}