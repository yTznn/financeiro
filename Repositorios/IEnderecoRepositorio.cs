using System.Collections.Generic;
using System.Threading.Tasks;
using Financeiro.Models;
using Financeiro.Models.ViewModels;

namespace Financeiro.Repositorios
{
    /// <summary>
    /// Repositório de Endereços (tabela <c>Endereco</c>) e operações relacionadas a
    /// vínculos com Pessoa Jurídica (tabela <c>PessoaEndereco</c>).
    /// </summary>
    public interface IEnderecoRepositorio
    {
        /* ===================== LEGADO (único endereço PJ) ===================== */

        /// <summary>
        /// Retorna o endereço vinculado à pessoa jurídica; null se não existir
        /// (legado — cenário de único endereço).
        /// </summary>
        Task<Endereco?> ObterPorPessoaAsync(int pessoaJuridicaId);

        /// <summary>
        /// Insere endereço novo (tabela <c>Endereco</c>) e cria o vínculo em <c>PessoaEndereco</c>.
        /// (Mantido para compatibilidade com o fluxo legado.)
        /// </summary>
        Task InserirAsync(EnderecoViewModel vm);

        /// <summary>
        /// Atualiza o endereço existente na tabela <c>Endereco</c> (dados básicos).
        /// </summary>
        Task AtualizarAsync(int id, EnderecoViewModel vm);

        /* ===================== NOVO (múltiplos endereços PJ) ===================== */

        /// <summary>
        /// Lista todos os endereços vinculados a uma Pessoa Jurídica (sugestão: principal primeiro).
        /// </summary>
        Task<IEnumerable<Endereco>> ListarPorPessoaJuridicaAsync(int pessoaJuridicaId);

        /// <summary>
        /// Retorna o endereço principal de uma Pessoa Jurídica (ou null se não houver).
        /// </summary>
        Task<Endereco?> ObterPrincipalPorPessoaJuridicaAsync(int pessoaJuridicaId);

        /// <summary>
        /// Define um endereço como principal para a Pessoa Jurídica (troca atômica).
        /// Deve limpar o principal anterior (se houver) e marcar o novo como principal.
        /// </summary>
        Task DefinirPrincipalPessoaJuridicaAsync(int pessoaJuridicaId, int enderecoId);

        /// <summary>
        /// Cria o vínculo em <c>PessoaEndereco</c> (Principal = 0 por padrão).
        /// Útil quando o endereço foi inserido separadamente.
        /// </summary>
        Task VincularPessoaJuridicaAsync(int pessoaJuridicaId, int enderecoId, bool ativo = true);

        /// <summary>
        /// Indica se a Pessoa Jurídica já possui um endereço principal.
        /// </summary>
        Task<bool> PossuiPrincipalPessoaJuridicaAsync(int pessoaJuridicaId);

        /* ===================== UTILIDADE (reuso geral) ===================== */

        /// <summary>
        /// Insere um registro em <c>Endereco</c> e retorna o Id gerado.
        /// (Sem criar vínculos.)
        /// </summary>
        Task<int> InserirRetornandoIdAsync(Endereco endereco);
    }
}