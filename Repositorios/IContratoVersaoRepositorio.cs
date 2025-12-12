using System.Collections.Generic;
using System.Threading.Tasks;
using Financeiro.Models;

namespace Financeiro.Repositorios
{
    public interface IContratoVersaoRepositorio
    {
        // Alterado para retornar int (ID da versão)
        Task<int> InserirAsync(ContratoVersao versao);
        
        // Insere a lista inicial de naturezas no histórico
        Task InserirNaturezasHistoricoAsync(int contratoVersaoId, IEnumerable<ContratoVersaoNatureza> itens);

        // [CORREÇÃO] Este é o método que estava faltando na Interface!
        // Ele permite atualizar o snapshot do histórico quando o usuário corrige o rateio na tela
        Task RecriarNaturezasHistoricoAsync(int contratoVersaoId, IEnumerable<ContratoVersaoNatureza> novosItens);

        Task<ContratoVersao?> ObterVersaoAtualAsync(int contratoId);
        Task<IEnumerable<ContratoVersao>> ListarPorContratoAsync(int contratoId);
        Task<(IEnumerable<ContratoVersao> Itens, int TotalPaginas)> ListarPaginadoAsync(int contratoId, int pagina, int tamanhoPagina = 5);
        Task<int> ContarPorContratoAsync(int contratoId);
        Task ExcluirAsync(int versaoId);
        Task RestaurarContratoAPartirDaVersaoAsync(ContratoVersao versaoAnterior);
    }
}