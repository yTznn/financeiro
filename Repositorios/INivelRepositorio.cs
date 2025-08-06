using Financeiro.Models.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Financeiro.Repositorios
{
    public interface INivelRepositorio
    {
        Task<IEnumerable<NivelDto>> BuscarAsync(string termo, int? nivel = null, bool incluirInativos = false);
        Task<bool> ExisteNomeAsync(string nome, int? idIgnorar = null);
        Task<int> InserirAsync(NivelDto dto);
        Task AtualizarAsync(NivelDto dto);
        Task InativarAsync(int id);
        Task<NivelDto> ObterPorIdAsync(int id);
    }
}