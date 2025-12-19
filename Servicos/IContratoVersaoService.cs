using System.Threading.Tasks;
using Financeiro.Models;
using Financeiro.Models.ViewModels;

namespace Financeiro.Servicos
{
    public interface IContratoVersaoService
    {
        // Cria a V1 quando o contrato nasce
        Task CriarVersaoInicialAsync(ContratoViewModel vm);

        // Usado pelo AditivosContratoController para criar o registro de aditivo
        Task CriarAditivoAsync(AditivoContratoViewModel vm);

        // Sincroniza o histórico após o usuário corrigir o rateio na edição
        Task AtualizarSnapshotUltimaVersaoAsync(int contratoId);

        // Cancela o último aditivo e restaura os dados anteriores (Header + Itens)
        Task<(ContratoVersao Removida, ContratoVersao? Vigente)> CancelarUltimoAditivoAsync(int contratoId, int versao, string justificativa);
    }
}