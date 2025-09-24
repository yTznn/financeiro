using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Financeiro.Models;

namespace Financeiro.Repositorios
{
    /// <summary>
    /// Repositório legado para aditivos, encaminhando para IInstrumentoVersaoRepositorio.
    /// </summary>
    public class AditivoRepositorio : IAditivoRepositorio
    {
        private readonly IInstrumentoVersaoRepositorio _inner;

        public AditivoRepositorio(IInstrumentoVersaoRepositorio inner)
        {
            _inner = inner;
        }

        public Task InserirAsync(InstrumentoVersao versao)
            => _inner.InserirAsync(versao);

        public Task<IEnumerable<InstrumentoVersao>> ListarPorInstrumentoAsync(int instrumentoId)
            => _inner.ListarPorInstrumentoAsync(instrumentoId);

        public Task<InstrumentoVersao?> ObterVersaoAtualAsync(int instrumentoId)
            => _inner.ObterVersaoAtualAsync(instrumentoId);

        public Task<(IEnumerable<InstrumentoVersao> Itens, int TotalPaginas)> ListarPaginadoAsync(int instrumentoId, int pagina, int tamPag = 5)
            => _inner.ListarPaginadoAsync(instrumentoId, pagina, tamPag);

        public Task ExcluirAsync(int versaoId)
            => _inner.ExcluirAsync(versaoId);

        public Task<InstrumentoVersao?> ObterVersaoAnteriorAsync(int instrumentoId, int versaoAtual)
            => _inner.ObterVersaoAnteriorAsync(instrumentoId, versaoAtual);

        public Task AtualizarVigenciaFimAsync(int versaoId, DateTime? dataFim)
            => _inner.AtualizarVigenciaFimAsync(versaoId, dataFim);

        public Task AtualizarDetalhesAsync(int versaoId, decimal novoValor, TipoAditivo tipoAditivo, string? observacao, DateTime? dataAssinatura)
            => _inner.AtualizarDetalhesAsync(versaoId, novoValor, tipoAditivo, observacao, dataAssinatura);
    }
}