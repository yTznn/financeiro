using System.Collections.Generic;
using System.Threading.Tasks;
using Financeiro.Models;
using Financeiro.Models.ViewModels;

namespace Financeiro.Repositorios
{
    public interface INaturezaRepositorio
    {
        Task InserirAsync(NaturezaViewModel vm);
        Task AtualizarAsync(int id, NaturezaViewModel vm);
        Task<Natureza?> ObterPorIdAsync(int id);
        
        // Alterado para aceitar um limite opcional
        Task<IEnumerable<Natureza>> ListarAsync(int? limite = null);
        
        Task<IEnumerable<Natureza>> ListarTodasAsync();
        Task<(IEnumerable<Natureza> Itens, int Total)> ListarPaginadoAsync(int pagina, int tamanho);
    }
}