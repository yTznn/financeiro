using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Financeiro.Models.ViewModels
{
    public class OrcamentoViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome do orçamento é obrigatório.")]
        [Display(Name = "Nome do Orçamento")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "É obrigatório vincular um Termo de Acordo.")]
        [Display(Name = "Termo de Acordo")]
        public int TipoAcordoId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Início da Vigência")]
        public DateTime VigenciaInicio { get; set; } = DateTime.Today;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Fim da Vigência")]
        public DateTime VigenciaFim { get; set; } = DateTime.Today.AddMonths(1);

        [Required]
        [Display(Name = "Valor Total Previsto")]
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser positivo.")]
        public decimal ValorPrevistoTotal { get; set; }

        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;
        
        [Display(Name = "Observação")]
        public string? Observacao { get; set; }

        public List<OrcamentoDetalheViewModel> Detalhamento { get; set; } = new();
    }
}