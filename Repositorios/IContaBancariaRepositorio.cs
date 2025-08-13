using System.Collections.Generic;
using System.Threading.Tasks;
using Financeiro.Models;
using Financeiro.Models.ViewModels;

namespace Financeiro.Repositorios
{
    /// <summary>
    /// Repositório de contas bancárias com suporte a múltiplos vínculos (PF/PJ) e apenas uma principal por pessoa.
    /// A "principalidade" é marcada na tabela de junção PessoaConta (IsPrincipal).
    /// </summary>
    public interface IContaBancariaRepositorio
    {
        /* ===================== CONSULTAS ===================== */

        /// <summary>Lista todas as contas vinculadas a uma Pessoa Física (ordem: principal primeiro).</summary>
        Task<IEnumerable<ContaBancariaViewModel>> ListarPorPessoaFisicaAsync(int pessoaFisicaId);

        /// <summary>Lista todas as contas vinculadas a uma Pessoa Jurídica (ordem: principal primeiro).</summary>
        Task<IEnumerable<ContaBancariaViewModel>> ListarPorPessoaJuridicaAsync(int pessoaJuridicaId);

        /// <summary>Obtém a conta principal (se existir) de uma Pessoa Física.</summary>
        Task<ContaBancariaViewModel?> ObterPrincipalPorPessoaFisicaAsync(int pessoaFisicaId);

        /// <summary>Obtém a conta principal (se existir) de uma Pessoa Jurídica.</summary>
        Task<ContaBancariaViewModel?> ObterPrincipalPorPessoaJuridicaAsync(int pessoaJuridicaId);

        /// <summary>Obtém uma conta bancária por ID (sem contexto de pessoa). Não inclui IsPrincipal.</summary>
        Task<ContaBancariaViewModel?> ObterContaPorIdAsync(int contaBancariaId);

        /// <summary>Obtém um vínculo PessoaConta por ID, retornando a conta + flags do vínculo.</summary>
        Task<ContaBancariaViewModel?> ObterVinculoPorIdAsync(int vinculoId);

        /// <summary>
        /// ✅ Compatibilidade: obtém a conta principal (se existir) de uma PF como entidade.
        /// Outros controllers antigos esperam este método.
        /// </summary>
        Task<ContaBancaria?> ObterPorPessoaFisicaAsync(int pessoaFisicaId);

        /// <summary>
        /// ✅ Compatibilidade: obtém a conta principal (se existir) de uma PJ como entidade.
        /// Outros controllers antigos esperam este método.
        /// </summary>
        Task<ContaBancaria?> ObterPorPessoaJuridicaAsync(int pessoaJuridicaId);

        /* ===================== GRAVAÇÃO ====================== */

        Task<int> InserirEVincularAsync(ContaBancariaViewModel vm);
        Task AtualizarContaAsync(int contaBancariaId, ContaBancariaViewModel vm);
        Task DefinirPrincipalAsync(int vinculoId);
        Task RemoverVinculoAsync(int vinculoId, bool removerContaSeOrfa = false);
    }
}