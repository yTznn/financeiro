using System.Collections.Generic;
using System.Threading.Tasks;
using Financeiro.Models;
using Financeiro.Models.Dto; // <--- Importando a pasta correta

namespace Financeiro.Repositorios
{
    public interface IPermissaoRepositorio
    {
        // --- MÉTODOS DE SEGURANÇA (LOGIN/ATRIBUTOS) ---
        Task<HashSet<string>> ObterPermissoesDoUsuarioAsync(int usuarioId);
        Task<IEnumerable<Permissao>> ListarTodasAsync();

        // --- MÉTODOS DE GESTÃO (TELA DE USUÁRIOS) ---
        
        // Retorna a lista completa para montar o checklist na tela
        Task<IEnumerable<PermissaoStatusDto>> ObterStatusPermissoesUsuarioAsync(int usuarioId);

        // Salva as alterações
        Task AtualizarPermissoesUsuarioAsync(int usuarioId, List<int> permissoesIds);
    }
}