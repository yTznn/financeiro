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

        // [NOVO] Método essencial para sincronizar o histórico após o usuário corrigir o rateio na edição
        Task AtualizarSnapshotUltimaVersaoAsync(int contratoId);

        // Retorna a tupla com a versão removida e a nova vigente
        Task<(ContratoVersao removida, ContratoVersao? vigente)> CancelarUltimoAditivoAsync(
            int contratoId, int versao, string justificativa);
    }
}