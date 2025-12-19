using System.Collections.Generic;
using System.Threading.Tasks;
using Financeiro.Models;

namespace Financeiro.Repositorios
{
    public interface IContratoVersaoRepositorio
    {
        // CRUD Básico da Versão (Cabeçalho)
        Task<int> InserirAsync(ContratoVersao versao);
        Task AtualizarAsync(ContratoVersao versao);
        Task ExcluirAsync(int id);
        
        // Consultas
        Task<ContratoVersao?> ObterVersaoAtualAsync(int contratoId);
        Task<ContratoVersao?> ObterPorIdAsync(int contratoId, int versao);
        Task<IEnumerable<ContratoVersao>> ListarPorContratoAsync(int contratoId);
        Task<(IEnumerable<ContratoVersao> Itens, int TotalPaginas)> ListarPaginadoAsync(int contratoId, int pagina, int tamanho = 10);
        Task<int> ContarPorContratoAsync(int contratoId);

        // --- MÉTODOS DE ITENS (SUBSTITUEM AS NATUREZAS) ---
        
        // Insere a lista de itens no histórico (Snapshot)
        Task InserirItensAsync(List<ContratoVersaoItem> itens);
        
        // Remove todos os itens de uma versão específica (para recriar o snapshot)
        Task ExcluirItensPorVersaoAsync(int contratoVersaoId);
        
        // Lista os itens de uma versão histórica (para exibir no modal ou restaurar backup)
        Task<IEnumerable<ContratoVersaoItem>> ListarItensPorVersaoAsync(int contratoVersaoId);
    }
}