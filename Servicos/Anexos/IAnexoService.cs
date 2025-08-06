using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Financeiro.Servicos.Anexos
{
    /// <summary>
    /// Serviço de anexo reutilizável em todo o sistema (perfil, contratos, documentos etc.).
    /// </summary>
    public interface IAnexoService
    {
        /// <summary>
        /// Processa e salva o anexo no banco de dados.
        /// Valida extensão, nome, tamanho e calcula o hash.
        /// </summary>
        /// <param name="arquivo">Arquivo enviado via form</param>
        /// <param name="origem">Ex: "PerfilUsuario", "Contrato"</param>
        /// <param name="chaveReferencia">Id da entidade associada (ex: Usuario.Id)</param>
        /// <returns>Id do anexo salvo no banco</returns>
        Task<int> SalvarAnexoAsync(IFormFile arquivo, string origem, int? chaveReferencia = null);
    }
}