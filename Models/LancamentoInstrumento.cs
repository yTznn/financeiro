namespace Financeiro.Models
{
    public class LancamentoInstrumento
    {
        public int Id { get; set; }
        public int InstrumentoId { get; set; }
        public DateTime Competencia { get; set; }   // 1º dia do mês
        public decimal Valor { get; set; }
        public string? Observacao { get; set; }
        public DateTime DataLancamento { get; set; }
    }
}