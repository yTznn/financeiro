using System.Threading.Tasks;
using Financeiro.Models.ViewModels;

namespace Financeiro.Servicos
{
    /// <summary>
    /// Define a lógica de negócio para gerenciar aditivos de Contrato.
    /// </summary>
    public interface IContratoVersaoService
    {
        /// <summary>
        /// Orquestra a criação de um aditivo, incluindo a criação da versão original se necessário.
        /// </summary>
        Task CriarAditivoAsync(AditivoContratoViewModel vm);
    }
}