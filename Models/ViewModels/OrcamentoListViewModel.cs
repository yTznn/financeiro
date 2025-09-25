// Financeiro/Models/ViewModels/OrcamentoListViewModel.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace Financeiro.Models.ViewModels
{
    public class OrcamentoListViewModel
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Observacao { get; set; }
        public DateTime VigenciaInicio { get; set; }
        public DateTime VigenciaFim { get; set; }
        
        [Display(Name = "Valor Orçado")]
        public decimal ValorPrevistoTotal { get; set; }
        public bool Ativo { get; set; }

        [Display(Name = "Valor Comprometido")]
        public decimal ValorComprometido { get; set; }

        [Display(Name = "Saldo Disponível")]
        public decimal SaldoDisponivel { get; set; }
    }
}