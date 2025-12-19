namespace Financeiro.Models
{
    /// <summary>
    /// Representa a tabela ContratoItem (1 Contrato tem N Itens)
    /// </summary>
    public class ContratoItem
    {
        public int Id { get; set; }
        public int ContratoId { get; set; }
        public int OrcamentoDetalheId { get; set; }
        public decimal Valor { get; set; }

        // Propriedades auxiliares para exibição (JOIN)
        public string? NomeItemOrcamento { get; set; }
    }
}