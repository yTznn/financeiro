using System.Collections.Generic;
using System.Threading.Tasks;
using Financeiro.Models;
using Financeiro.Models.ViewModels;

namespace Financeiro.Repositorios
{
    public interface IPessoaFisicaRepositorio
    {
        Task InserirAsync(PessoaFisicaViewModel vm);
        Task<IEnumerable<PessoaFisica>> ListarAsync();
        Task<PessoaFisica?> ObterPorIdAsync(int id);
        Task AtualizarAsync(int id, PessoaFisicaViewModel vm);
        Task<PessoaFisica?> ObterPorCpfAsync(string cpf);
        Task<bool> ExisteContratoPorPessoaFisicaAsync(int pessoaFisicaId);
        Task ExcluirAsync(int id);
    }
}