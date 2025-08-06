using System.Threading.Tasks;
using Financeiro.Models;
using Financeiro.Models.ViewModels;

namespace Financeiro.Repositorios
{
    public interface IEnderecoRepositorio
    {
        /// <summary>Retorna o endereço vinculado à pessoa; null se não existir.</summary>
        Task<Endereco?> ObterPorPessoaAsync(int pessoaJuridicaId);

        /// <summary>Insere endereço novo e faz o vínculo PessoaEndereco.</summary>
        Task InserirAsync(EnderecoViewModel vm);

        /// <summary>Atualiza o endereço existente (já vinculado).</summary>
        Task AtualizarAsync(int id, EnderecoViewModel vm);
    }
}