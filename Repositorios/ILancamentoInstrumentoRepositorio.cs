// Repositorios/ILancamentoInstrumentoRepositorio.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Financeiro.Models;

namespace Financeiro.Repositorios
{
    public interface ILancamentoInstrumentoRepositorio
    {
        Task InserirAsync(LancamentoInstrumento m);
        Task AtualizarAsync(int id, decimal valor, string? observacao);
        Task ExcluirAsync(int id);

        Task<LancamentoInstrumento?> ObterPorIdAsync(int id);

        Task<IEnumerable<LancamentoInstrumento>> ListarPorInstrumentoAsync(
            int instrumentoId, DateTime? de = null, DateTime? ate = null);

        Task<decimal> SomatorioNoMesAsync(int instrumentoId, DateTime competenciaPrimeiroDia);
    }
}