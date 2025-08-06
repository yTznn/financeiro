using System.Threading.Tasks;
using Financeiro.Models;
using Financeiro.Models.ViewModels;

namespace Financeiro.Repositorios
{
    public interface IContaBancariaRepositorio
    {
        /* -------- Consultas -------- */

        /// <summary>Conta vinculada a uma Pessoa Jurídica; null se não existir.</summary>
        Task<ContaBancaria?> ObterPorPessoaJuridicaAsync(int pessoaJuridicaId);

        /// <summary>Conta vinculada a uma Pessoa Física; null se não existir.</summary>
        Task<ContaBancaria?> ObterPorPessoaFisicaAsync(int pessoaFisicaId);

        /* -------- Gravação -------- */

        /// <summary>Insere conta nova e cria vínculo (PJ ou PF indicado no ViewModel).</summary>
        Task InserirAsync(ContaBancariaViewModel vm);

        /// <summary>Atualiza conta existente.</summary>
        Task AtualizarAsync(int id, ContaBancariaViewModel vm);
    }
}