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
        Task<PessoaJuridica?> ObterPorIdAsync(int id);
        Task AtualizarAsync(int id, PessoaJuridicaViewModel vm);
        Task ExcluirAsync(int id);
        Task<PessoaJuridica?> ObterPorCnpjAsync(string cnpj);
        Task<(IEnumerable<PessoaJuridica> Lista, int TotalRegistros)> ListarPaginadoAsync(int paginaAtual, int itensPorPagina);

    }
}