using System.Threading.Tasks;

namespace Financeiro.Servicos
{
    public interface IJustificativaService
    {
        Task RegistrarAsync(int usuarioId, int entidadeId, string tabela, string acao, int registroId, string texto);
    }
}