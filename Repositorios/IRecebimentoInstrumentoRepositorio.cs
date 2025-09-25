using Financeiro.Models;
using Financeiro.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Financeiro.Repositorios
{
    public interface IRecebimentoInstrumentoRepositorio
    {
        Task<RecebimentoViewModel?> ObterParaEdicaoAsync(int id);
        Task<IEnumerable<RecebimentoViewModel>> ListarPorInstrumentoAsync(int instrumentoId);
        Task<int> InserirAsync(RecebimentoViewModel vm);
        Task AtualizarAsync(RecebimentoViewModel vm);
        Task ExcluirAsync(int id);
        Task<IEnumerable<RecebimentoViewModel>> ListarTodosAsync();

    }
}