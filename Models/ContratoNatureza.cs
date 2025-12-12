namespace Financeiro.Models
{
    /// <summary>
    /// Representa a tabela de ligação ContratoNatureza.
    /// </summary>
    public class ContratoNatureza
    {
        public int ContratoId { get; set; }
        public int NaturezaId { get; set; }
        public decimal Valor { get; set; }
    }
}