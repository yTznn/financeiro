using Financeiro.Models;
using System.Threading.Tasks;

namespace Financeiro.Repositorios
{
    public interface IArquivoRepositorio
    {
        Task<int> AdicionarAsync(Arquivo arquivo);
        Task<Arquivo?> ObterPorIdAsync(int id);
        Task<Arquivo?> ObterPorHashAsync(string hash);
        Task<Arquivo?> ObterPorReferenciaAsync(string origem, int chaveReferencia);

    }
}