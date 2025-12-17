using Financeiro.Models;
using System.Threading.Tasks;
using Financeiro.Models.ViewModels;

namespace Financeiro.Repositorios
{
    public interface ILogRepositorio
    {
        Task RegistrarAsync(Log log);
        Task<(IEnumerable<LogListagemViewModel> Itens, int Total)> ListarLogsPaginadosAsync(int pagina, int tamanho, int? usuarioId);
        Task<IEnumerable<dynamic>> BuscarUsuariosParaSelectAsync(string termo);
    }
}