namespace Financeiro.Models
{
    public class MovimentacaoRateio
    {
        public int Id { get; set; }
        public int MovimentacaoId { get; set; }
        public int? ContratoId { get; set; }
        public int InstrumentoId { get; set; }
        
        // MUDANÇA: Sai NaturezaId, Entra OrcamentoDetalheId
        public int OrcamentoDetalheId { get; set; } 
        
        public decimal Valor { get; set; }
    }
}