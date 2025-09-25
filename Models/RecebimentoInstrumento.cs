using System;

namespace Financeiro.Models
{
    public class RecebimentoInstrumento
    {
        public int Id { get; set; }
        public int InstrumentoId { get; set; }
        public decimal Valor { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public string? Observacao { get; set; }
        public DateTime DataCriacao { get; set; }
    }
}