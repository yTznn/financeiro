using System.Collections.Generic;
using System.Threading.Tasks;
using Financeiro.Models;
using Financeiro.Models.ViewModels;

namespace Financeiro.Servicos
{
    /// <summary>
    /// Orquestra operações de endereço para Pessoas Jurídicas e Físicas.
    /// Mantém regra de negócios no nível de serviço e delega persistência ao repositório.
    /// </summary>
    public interface IEnderecoService
    {
        /* ===================== LEGADO (único endereço PJ) ===================== */
        Task<Endereco?> ObterPorPessoaAsync(int pessoaJuridicaId);
        Task InserirAsync(EnderecoViewModel vm);
        Task AtualizarAsync(int id, EnderecoViewModel vm);

        /* ===================== PJ (múltiplos endereços) ===================== */
        Task<IEnumerable<Endereco>> ListarPorPessoaJuridicaAsync(int pessoaJuridicaId);
        Task<Endereco?> ObterPrincipalPorPessoaJuridicaAsync(int pessoaJuridicaId);
        Task DefinirPrincipalPessoaJuridicaAsync(int pessoaJuridicaId, int enderecoId);
        Task VincularPessoaJuridicaAsync(int pessoaJuridicaId, int enderecoId, bool ativo = true);
        Task<bool> PossuiPrincipalPessoaJuridicaAsync(int pessoaJuridicaId);

        /* ===================== PF (múltiplos endereços) ===================== */
        Task<IEnumerable<Endereco>> ListarPorPessoaFisicaAsync(int pessoaFisicaId);
        Task<Endereco?> ObterPrincipalPorPessoaFisicaAsync(int pessoaFisicaId);
        Task DefinirPrincipalPessoaFisicaAsync(int pessoaFisicaId, int enderecoId);
        Task VincularPessoaFisicaAsync(int pessoaFisicaId, int enderecoId, bool ativo = true);
        Task<bool> PossuiPrincipalPessoaFisicaAsync(int pessoaFisicaId);

        /* ===================== UTILIDADE (reuso geral) ===================== */
        Task<int> InserirRetornandoIdAsync(Endereco endereco);

        // NOVO
        Task<Endereco?> ObterPorIdAsync(int enderecoId);
        Task ExcluirEnderecoPessoaJuridicaAsync(int pessoaJuridicaId, int enderecoId);
        Task ExcluirEnderecoPessoaFisicaAsync(int pessoaFisicaId, int enderecoId);
    }
}