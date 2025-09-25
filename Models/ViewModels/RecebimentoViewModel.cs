using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Financeiro.Models.ViewModels
{
    public class RecebimentoViewModel : IValidatableObject
    {
        public int Id { get; set; }

        // Este campo será preenchido pelo sistema, não pelo usuário diretamente
        public int InstrumentoId { get; set; }

        [Required(ErrorMessage = "O valor é obrigatório.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero.")]
        [Display(Name = "Valor Recebido")]
        public decimal Valor { get; set; }

        [Required(ErrorMessage = "A data de início do período é obrigatória.")]
        [DataType(DataType.Date)]
        [Display(Name = "Início do Período")]
        public DateTime DataInicio { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "A data de fim do período é obrigatória.")]
        [DataType(DataType.Date)]
        [Display(Name = "Fim do Período")]
        public DateTime DataFim { get; set; } = DateTime.Today;

        [Display(Name = "Observação")]
        [StringLength(1000, ErrorMessage = "A observação não pode exceder 1000 caracteres.")]
        public string? Observacao { get; set; }

        // Usado para exibir o nome do Instrumento na tela
        public string? InstrumentoNumero { get; set; }

        // Validação customizada para garantir que a data final não seja anterior à inicial
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (DataFim < DataInicio)
            {
                yield return new ValidationResult(
                    "A data de fim deve ser maior ou igual à data de início.",
                    new[] { nameof(DataFim) });
            }
        }
    }
}