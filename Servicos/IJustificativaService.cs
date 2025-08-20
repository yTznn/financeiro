using System.Threading.Tasks;

namespace Financeiro.Servicos
{
    public interface IJustificativaService
    {
        Task RegistrarAsync(string tabela, string acao, int registroId, string texto);
    }
}