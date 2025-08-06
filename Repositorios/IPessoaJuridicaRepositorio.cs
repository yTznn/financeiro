// Repositorios/IPessoaJuridicaRepositorio.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using Financeiro.Models;
using Financeiro.Models.ViewModels;

namespace Financeiro.Repositorios
{
    public interface IPessoaJuridicaRepositorio
    {
        Task InserirAsync(PessoaJuridicaViewModel vm);
        Task<IEnumerable<PessoaJuridica>> ListarAsync();

        /* novos métodos */
        Task<PessoaJuridica?> ObterPorIdAsync(int id);
        Task AtualizarAsync(int id, PessoaJuridicaViewModel vm);
    }
}