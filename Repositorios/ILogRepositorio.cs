using Financeiro.Models;
using System.Threading.Tasks;

namespace Financeiro.Repositorios
{
    public interface ILogRepositorio
    {
        Task RegistrarAsync(Log log);
    }
}