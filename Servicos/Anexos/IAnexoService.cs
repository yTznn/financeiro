using Financeiro.Models; // Necessário para retornar 'Arquivo'
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Financeiro.Servicos.Anexos
{
    public interface IAnexoService
    {
        Task<int> SalvarAnexoAsync(IFormFile arquivo, string origem, int? chaveReferencia = null);
        Task<Arquivo?> ObterPorReferenciaAsync(string origem, int chaveReferencia);
    }
}