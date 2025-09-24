using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Financeiro.Models;

namespace Financeiro.Repositorios
{
    /// <summary>
    /// Interface legado para aditivos de Instrumento.
    /// Mantida para compatibilidade, mas usando InstrumentoVersao.
    /// </summary>
    public interface IAditivoRepositorio
    {
        Task InserirAsync(InstrumentoVersao versao);
        Task<IEnumerable<InstrumentoVersao>> ListarPorInstrumentoAsync(int instrumentoId);
        Task<InstrumentoVersao?> ObterVersaoAtualAsync(int instrumentoId);
        Task<(IEnumerable<InstrumentoVersao> Itens, int TotalPaginas)> ListarPaginadoAsync(int instrumentoId, int pagina, int tamPag = 5);
        Task ExcluirAsync(int versaoId);

        Task<InstrumentoVersao?> ObterVersaoAnteriorAsync(int instrumentoId, int versaoAtual);
        Task AtualizarVigenciaFimAsync(int versaoId, DateTime? dataFim);
        Task AtualizarDetalhesAsync(int versaoId, decimal novoValor, TipoAditivo tipoAditivo, string? observacao, DateTime? dataAssinatura);
    }
}