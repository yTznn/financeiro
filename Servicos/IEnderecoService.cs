using System.Collections.Generic;
using System.Threading.Tasks;
using Financeiro.Models;
using Financeiro.Models.ViewModels;

namespace Financeiro.Servicos
{
    /// <summary>
    /// Orquestra operações de endereço para Pessoa Jurídica.
    /// Mantém regra de negócios no nível de serviço e delega persistência ao repositório.
    /// </summary>
    public interface IEnderecoService
    {
        /// <summary>Retorna um endereço vinculado à pessoa jurídica (legado – único). Prefira os novos métodos.</summary>
        Task<Endereco?> ObterPorPessoaAsync(int pessoaJuridicaId);

        /// <summary>Lista todos os endereços vinculados a uma Pessoa Jurídica (principal primeiro).</summary>
        Task<IEnumerable<Endereco>> ListarPorPessoaJuridicaAsync(int pessoaJuridicaId);

        /// <summary>Retorna o endereço principal da Pessoa Jurídica, se houver.</summary>
        Task<Endereco?> ObterPrincipalPorPessoaJuridicaAsync(int pessoaJuridicaId);

        /// <summary>Insere novo endereço e vincula à Pessoa Jurídica. Se não houver principal, marca este como principal.</summary>
        Task InserirAsync(EnderecoViewModel vm);

        /// <summary>Atualiza dados do endereço existente.</summary>
        Task AtualizarAsync(int id, EnderecoViewModel vm);

        /// <summary>Define um endereço como principal para a Pessoa Jurídica (troca atômica).</summary>
        Task DefinirPrincipalPessoaJuridicaAsync(int pessoaJuridicaId, int enderecoId);
    }
}