namespace Financeiro.Models.ViewModels
{
    public class ContratoNaturezaViewModel
    {
        public int NaturezaId { get; set; }
        public string NomeNatureza { get; set; } // Apenas para exibir na tela
        public decimal Valor { get; set; } // O valor rateado para esta natureza
    }
}