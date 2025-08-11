using System.Threading.Tasks;

namespace Financeiro.Servicos
{
    public interface ILogService
    {
        Task RegistrarCriacaoAsync(string tabela, object novoValor, int? registroId = null);
        Task RegistrarEdicaoAsync(string tabela, object valorAntigo, object valorNovo, int? registroId = null);
        Task RegistrarExclusaoAsync(string tabela, object valorAntigo, int? registroId = null);
    }
}