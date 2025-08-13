using System.Collections.Generic;
using System.Threading.Tasks;
using Financeiro.Models;
using Financeiro.Models.ViewModels;

namespace Financeiro.Repositorios
{
    /// <summary>
    /// Repositório de Endereços (tabela <c>Endereco</c>) e operações relacionadas a
    /// vínculos com Pessoa Jurídica e Pessoa Física (tabela <c>PessoaEndereco</c>).
    /// </summary>
    public interface IEnderecoRepositorio
    {
        /* ===================== LEGADO (único endereço PJ) ===================== */

        Task<Endereco?> ObterPorPessoaAsync(int pessoaJuridicaId);
        Task InserirAsync(EnderecoViewModel vm);
        Task AtualizarAsync(int id, EnderecoViewModel vm);

        /* ===================== NOVO (múltiplos endereços PJ) ===================== */

        Task<IEnumerable<Endereco>> ListarPorPessoaJuridicaAsync(int pessoaJuridicaId);
        Task<Endereco?> ObterPrincipalPorPessoaJuridicaAsync(int pessoaJuridicaId);
        Task DefinirPrincipalPessoaJuridicaAsync(int pessoaJuridicaId, int enderecoId);
        Task VincularPessoaJuridicaAsync(int pessoaJuridicaId, int enderecoId, bool ativo = true);
        Task<bool> PossuiPrincipalPessoaJuridicaAsync(int pessoaJuridicaId);

        /* ===================== NOVO (múltiplos endereços PF) ===================== */

        Task<IEnumerable<Endereco>> ListarPorPessoaFisicaAsync(int pessoaFisicaId);
        Task<Endereco?> ObterPrincipalPorPessoaFisicaAsync(int pessoaFisicaId);
        Task DefinirPrincipalPessoaFisicaAsync(int pessoaFisicaId, int enderecoId);
        Task VincularPessoaFisicaAsync(int pessoaFisicaId, int enderecoId, bool ativo = true);
        Task<bool> PossuiPrincipalPessoaFisicaAsync(int pessoaFisicaId);

        /* ===================== UTILIDADE (reuso geral) ===================== */

        Task<int> InserirRetornandoIdAsync(Endereco endereco);

        // NOVO: utilidades para editar/excluir por EnderecoId (PF e PJ)
        Task<Endereco?> ObterPorIdAsync(int enderecoId); // NOVO
        Task ExcluirEnderecoPessoaJuridicaAsync(int pessoaJuridicaId, int enderecoId); // NOVO
        Task ExcluirEnderecoPessoaFisicaAsync(int pessoaFisicaId, int enderecoId);     // NOVO
    }
}