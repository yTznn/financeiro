using System.Collections.Generic;
using System.Threading.Tasks;
using Financeiro.Models;

namespace Financeiro.Repositorios
{
    public interface IAditivoRepositorio
    {
        /// <summary>Insere um novo registro de versão (aditivo).</summary>
        Task InserirAsync(AcordoVersao versao);

        /// <summary>Retorna todas as versões de um acordo, ordenadas pela versão.</summary>
        Task<IEnumerable<AcordoVersao>> ListarPorAcordoAsync(int tipoAcordoId);

        /// <summary>Retorna a versão vigente (VigenciaFim = null) do acordo.</summary>
        Task<AcordoVersao?> ObterVersaoAtualAsync(int tipoAcordoId);
        Task<(IEnumerable<AcordoVersao> itens, int totalPaginas)> ListarPaginadoAsync(int acordoId, int pagina, int tamPag = 5);

    }
}