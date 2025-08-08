using Financeiro.Models;

namespace Financeiro.Repositorios
{
    public interface IUsuarioEntidadeRepositorio
    {
        Task<IEnumerable<UsuarioEntidade>> ListarPorUsuarioAsync(int usuarioId);
        Task InserirAsync(UsuarioEntidade vinculo);
        Task AtualizarAtivoAsync(int usuarioId, int entidadeIdAtiva);
        Task RemoverTodosPorUsuarioAsync(int usuarioId);
    }
}