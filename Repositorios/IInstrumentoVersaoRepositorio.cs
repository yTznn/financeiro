using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Financeiro.Models;

namespace Financeiro.Repositorios
{
    /// <summary>
    /// Operações sobre o histórico de versões (aditivos) do Instrumento.
    /// Mapeia a tabela dbo.InstrumentoVersao (coluna InstrumentoId).
    /// </summary>
    public interface IInstrumentoVersaoRepositorio
    {
        /// <summary>Insere uma nova versão/aditivo.</summary>
        Task InserirAsync(InstrumentoVersao versao);

        /// <summary>Retorna todas as versões de um Instrumento, ordenadas por versão (DESC).</summary>
        Task<IEnumerable<InstrumentoVersao>> ListarPorInstrumentoAsync(int instrumentoId);

        /// <summary>Retorna a versão vigente (VigenciaFim = NULL) do Instrumento.</summary>
        Task<InstrumentoVersao?> ObterVersaoAtualAsync(int instrumentoId);

        /// <summary>Lista paginada do histórico de versões/aditivos.</summary>
        Task<(IEnumerable<InstrumentoVersao> Itens, int TotalPaginas)> ListarPaginadoAsync(int instrumentoId, int pagina, int tamPag = 5);

        /// <summary>Exclui uma versão específica (por Id).</summary>
        Task ExcluirAsync(int versaoId);

        Task<InstrumentoVersao?> ObterVersaoAnteriorAsync(int instrumentoId, int versaoAtual);

        Task AtualizarVigenciaFimAsync(int versaoId, DateTime? dataFim);

        /// <summary>Atualiza detalhes de uma versão existente (ex.: aditivo no mesmo dia).</summary>
        Task AtualizarDetalhesAsync(int versaoId, decimal novoValor, TipoAditivo tipoAditivo, string? observacao, DateTime? dataAssinatura);
    }
}