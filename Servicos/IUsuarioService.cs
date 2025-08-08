using Financeiro.Models;

namespace Financeiro.Servicos
{
    public interface IUsuarioService
    {
        // … métodos que você já tem (CriarAsync, AtualizarAsync, etc.)

        /// <summary>Grava vínculos de entidades para o usuário e define a ativa.</summary>
        Task SalvarEntidadesAsync(int usuarioId, IEnumerable<int> entidadesSelecionadas, int entidadeAtivaId);

        /// <summary>Retorna as entidades já vinculadas ao usuário.</summary>
        Task<IEnumerable<UsuarioEntidade>> ListarEntidadesAsync(int usuarioId);
    }
}