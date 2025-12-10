using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Financeiro.Models.ViewModels
{
    public class InstrumentoViewModel : IValidatableObject
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Informe o número do instrumento.")]
        [Display(Name = "Número")]
        [StringLength(100, ErrorMessage = "Número muito longo (máx. {1} caracteres).")]
        public string Numero { get; set; } = string.Empty;

        // Valor TOTAL do instrumento. (Sem Range fixo — será validado de forma condicional.)
        [Display(Name = "Valor Total")]
        [DataType(DataType.Currency)]
        public decimal Valor { get; set; }

        [Display(Name = "Calcular pelo valor mensal?")]
        public bool UsarValorMensal { get; set; }

        // Valor mensal. (Sem Range fixo — será validado de forma condicional.)
        [Display(Name = "Valor Mensal")]
        [DataType(DataType.Currency)]
        public decimal? ValorMensal { get; set; }

        [Required(ErrorMessage = "Descreva o objeto do termo.")]
        [Display(Name = "Objeto do Termo")]
        [StringLength(4000, ErrorMessage = "Máximo de {1} caracteres.")]
        public string Objeto { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe a data de início.")]
        [DataType(DataType.Date)]
        [Display(Name = "Data Início")]
        public DateTime DataInicio { get; set; }

        [Required(ErrorMessage = "Informe a data de fim.")]
        [DataType(DataType.Date)]
        [Display(Name = "Data Fim")]
        public DateTime DataFim { get; set; }

        [Display(Name = "Ativo?")]
        public bool Ativo { get; set; }

        [Display(Name = "Vigente (Padrão)?")]
        public bool Vigente { get; set; }

        [Display(Name = "Observação")]
        public string? Observacao { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Data de Assinatura")]
        public DateTime? DataAssinatura { get; set; }

        [Required(ErrorMessage = "Selecione a Entidade.")]
        [Display(Name = "Entidade")]
        public int EntidadeId { get; set; }

        // ===== Validação condicional =====
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (UsarValorMensal)
            {
                if (!ValorMensal.HasValue || ValorMensal.Value <= 0)
                    yield return new ValidationResult(
                        "Valor mensal deve ser maior que zero.",
                        new[] { nameof(ValorMensal) });
            }
            else
            {
                if (Valor <= 0)
                    yield return new ValidationResult(
                        "Valor total deve ser maior que zero.",
                        new[] { nameof(Valor) });
            }
        }
    }
}