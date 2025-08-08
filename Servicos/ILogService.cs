using Financeiro.Models;
using System.Threading.Tasks;

namespace Financeiro.Servicos
{
    public interface ILogService
    {
        Task RegistrarCriacaoAsync(string tabela, object novoValor);
        Task RegistrarEdicaoAsync(string tabela, object valorAntigo, object valorNovo);
        Task RegistrarExclusaoAsync(string tabela, object valorAntigo);
    }
}