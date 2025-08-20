using System.Collections.Generic;
using System.Threading.Tasks;
using Financeiro.Models;

namespace Financeiro.Repositorios
{
    public interface IContratoVersaoRepositorio
    {
        Task InserirAsync(ContratoVersao versao);
        Task<ContratoVersao?> ObterVersaoAtualAsync(int contratoId);
        Task<IEnumerable<ContratoVersao>> ListarPorContratoAsync(int contratoId);
        Task<(IEnumerable<ContratoVersao> Itens, int TotalPaginas)> ListarPaginadoAsync(int contratoId, int pagina, int tamanhoPagina = 5);

        Task<int> ContarPorContratoAsync(int contratoId);
        Task ExcluirAsync(int versaoId);
        Task RestaurarContratoAPartirDaVersaoAsync(ContratoVersao versaoAnterior);
    }
}