using System.Collections.Generic;
using System.Threading.Tasks;
using Financeiro.Models;
using Financeiro.Models.ViewModels;

namespace Financeiro.Repositorios
{
    public interface IContaBancariaRepositorio
    {
        Task<IEnumerable<ContaBancariaViewModel>> ListarPorPessoaFisicaAsync(int pessoaFisicaId);
        Task<IEnumerable<ContaBancariaViewModel>> ListarPorPessoaJuridicaAsync(int pessoaJuridicaId);
        Task<ContaBancariaViewModel?> ObterPrincipalPorPessoaFisicaAsync(int pessoaFisicaId);
        Task<ContaBancariaViewModel?> ObterPrincipalPorPessoaJuridicaAsync(int pessoaJuridicaId);
        Task<ContaBancariaViewModel?> ObterContaPorIdAsync(int contaBancariaId);
        Task<ContaBancariaViewModel?> ObterVinculoPorIdAsync(int vinculoId);
        Task<ContaBancaria?> ObterPorPessoaFisicaAsync(int pessoaFisicaId);
        Task<ContaBancaria?> ObterPorPessoaJuridicaAsync(int pessoaJuridicaId);
        Task<int> InserirEVincularAsync(ContaBancariaViewModel vm);
        Task AtualizarContaAsync(int contaBancariaId, ContaBancariaViewModel vm);
        Task DefinirPrincipalAsync(int vinculoId);
        Task RemoverVinculoAsync(int vinculoId, bool removerContaSeOrfa = false);
        Task<int> InserirRetornandoIdAsync(ContaBancaria conta);
        Task<ContaBancaria?> ObterPorIdAsync(int id);
        Task AtualizarAsync(ContaBancaria conta);
    }
}