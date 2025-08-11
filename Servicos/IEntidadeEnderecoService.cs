using System.Collections.Generic;
using System.Threading.Tasks;
using Financeiro.Models;
using Financeiro.Models.ViewModels;

namespace Financeiro.Servicos
{
    /// <summary>
    /// Regras de negócio para endereços de Entidade (múltiplos + principal).
    /// </summary>
    public interface IEntidadeEnderecoService
    {
        /// <summary>Lista endereços vinculados à Entidade (principal primeiro).</summary>
        Task<IEnumerable<Endereco>> ListarPorEntidadeAsync(int entidadeId);

        /// <summary>Obtém o endereço principal da Entidade (ou null).</summary>
        Task<Endereco?> ObterPrincipalPorEntidadeAsync(int entidadeId);

        /// <summary>Define um endereço como principal para a Entidade (troca atômica + sincroniza Entidade.EnderecoId).</summary>
        Task DefinirPrincipalEntidadeAsync(int entidadeId, int enderecoId);

        /// <summary>
        /// Cria um novo endereço (tabela Endereco), vincula à Entidade (EntidadeEndereco)
        /// e, se solicitado (ou se for o primeiro), define como principal.
        /// Retorna (novoEnderecoId, principalAlterado).
        /// </summary>
        Task<(int enderecoId, bool principalAlterado)> CriarEnderecoParaEntidadeAsync(
            int entidadeId, EnderecoViewModel vm, bool definirComoPrincipal);
    }
}