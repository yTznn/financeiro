// Financeiro/Models/ViewModels/OrcamentoListViewModel.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace Financeiro.Models.ViewModels
{
    public class OrcamentoListViewModel
    {
        public int Id { get; set; }
        
        [Display(Name = "Nome do Orçamento")]
        public string Nome { get; set; } = string.Empty;
        
        public string? Observacao { get; set; }
        
        [Display(Name = "Vigência Início")]
        public DateTime VigenciaInicio { get; set; }
        
        [Display(Name = "Vigência Fim")]
        public DateTime VigenciaFim { get; set; }
        
        [Display(Name = "Valor Orçado")]
        public decimal ValorPrevistoTotal { get; set; }
        
        public bool Ativo { get; set; }

        [Display(Name = "Valor Comprometido")]
        public decimal ValorComprometido { get; set; }

        [Display(Name = "Saldo Disponível")]
        public decimal SaldoDisponivel { get; set; }

        // [NOVO] Propriedade adicionada para receber "Numero - Objeto" do SQL
        [Display(Name = "Instrumento")]
        public string InstrumentoNome { get; set; } = string.Empty;
    }
}