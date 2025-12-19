namespace Financeiro.Models
{
    public class ContratoVersaoItem
    {
        public int Id { get; set; }
        public int ContratoVersaoId { get; set; }
        public int OrcamentoDetalheId { get; set; }
        public decimal Valor { get; set; }

        // Propriedade auxiliar para leitura (não existe na tabela)
        public string? NomeItem { get; set; }
    }
}