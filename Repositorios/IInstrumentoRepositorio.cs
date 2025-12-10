using System.Collections.Generic;
using System.Threading.Tasks;
using Financeiro.Models;
using Financeiro.Models.ViewModels;

namespace Financeiro.Repositorios
{
    public interface IInstrumentoRepositorio
    {
        Task InserirAsync(InstrumentoViewModel vm);
        Task AtualizarAsync(int id, InstrumentoViewModel vm);
        Task<Instrumento?> ObterPorIdAsync(int id);
        
        // Mantemos o ListarAsync genérico por enquanto
        Task<IEnumerable<Instrumento>> ListarAsync(); 
        
        Task ExcluirAsync(int id);

        // --- ALTERAÇÃO AQUI: Adicionado 'entidadeId' para validar duplicidade apenas na unidade ---
        Task<bool> ExisteNumeroAsync(string numero, int entidadeId, int? ignorarId = null);
        
        Task<string> SugerirProximoNumeroAsync(int ano);

        // Retorna uma Tupla: (Lista de Itens, Total de Registros para o Pager)
        Task<(IEnumerable<InstrumentoResumoViewModel> Itens, int TotalItens)> ListarResumoPaginadoAsync(int entidadeId, int pagina, int tamanhoPagina);
        
        Task<InstrumentoResumoViewModel?> ObterResumoAsync(int instrumentoId);

        // Método auxiliar da lógica de vigência
        Task<Instrumento?> ObterVigentePorEntidadeAsync(int entidadeId);
    }
}