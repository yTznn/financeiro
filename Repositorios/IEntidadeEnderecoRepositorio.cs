using System.Collections.Generic;
using System.Threading.Tasks;
using Financeiro.Models;

namespace Financeiro.Repositorios
{
    /// <summary>
    /// Acesso a dados para o vínculo de endereços de uma Entidade (tabela EntidadeEndereco).
    /// Suporta múltiplos endereços, garantindo exatamente um principal por Entidade.
    /// </summary>
    public interface IEntidadeEnderecoRepositorio
    {
        /// <summary>Lista todos os endereços vinculados à Entidade (ativos), com o principal primeiro.</summary>
        Task<IEnumerable<Endereco>> ListarPorEntidadeAsync(int entidadeId);

        /// <summary>Retorna o endereço principal da Entidade (ou null se não houver).</summary>
        Task<Endereco?> ObterPrincipalPorEntidadeAsync(int entidadeId);

        /// <summary>
        /// Define um endereço como principal para a Entidade (troca atômica)
        /// e sincroniza <c>Entidade.EnderecoId</c>.
        /// </summary>
        Task DefinirPrincipalEntidadeAsync(int entidadeId, int enderecoId);

        /// <summary>
        /// Cria (se não existir) ou reativa o vínculo Entidade↔Endereço com <c>Principal = 0</c>.
        /// Não altera quem é o principal atual.
        /// </summary>
        Task VincularAsync(int entidadeId, int enderecoId, bool ativo = true);

        /// <summary>Indica se já existe endereço principal para a Entidade.</summary>
        Task<bool> PossuiPrincipalAsync(int entidadeId);

        /// <summary>
        /// Exclui o vínculo Entidade↔Endereço e, se o endereço não estiver
        /// mais vinculado a nenhuma outra entidade/pessoa, apaga-o da tabela <c>Endereco</c>.
        /// Se o endereço excluído era o principal, e ainda houver outros endereços,
        /// define um deles como principal; caso contrário, zera <c>Entidade.EnderecoId</c>.
        /// </summary>
        /// <returns><c>true</c> se o registro da tabela <c>Endereco</c> foi excluído; caso contrário, <c>false</c>.</returns>
        Task<bool> ExcluirAsync(int entidadeId, int enderecoId);
    }
}