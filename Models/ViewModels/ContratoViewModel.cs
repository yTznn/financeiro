using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Globalization; // Necessário para a conversão

namespace Financeiro.Models.ViewModels
{
    public class ContratoViewModel : IValidatableObject
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "É obrigatório selecionar um fornecedor.")]
        [Display(Name = "Fornecedor")]
        public string FornecedorIdCompleto { get; set; }

        [Required(ErrorMessage = "O número do contrato é obrigatório.")]
        public int NumeroContrato { get; set; }

        [Required(ErrorMessage = "O ano do contrato é obrigatório.")]
        public int AnoContrato { get; set; } = DateTime.Now.Year;

        [Required(ErrorMessage = "O objeto do contrato é obrigatório.")]
        public string ObjetoContrato { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime? DataAssinatura { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Início da Vigência")]
        public DateTime DataInicio { get; set; } = DateTime.Today;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Fim da Vigência")]
        public DateTime DataFim { get; set; } = DateTime.Today.AddYears(1);

        // [MUDANÇA RADICAL] Recebemos como TEXTO para o ModelBinder não zerar o valor
        [Required(ErrorMessage = "O valor mensal é obrigatório.")]
        [Display(Name = "Valor Mensal")]
        public string ValorMensal { get; set; } // Agora é STRING

        // Propriedade auxiliar que converte o texto para Decimal (uso interno no Controller)
        public decimal ValorMensalDecimal
        {
            get
            {
                if (string.IsNullOrEmpty(ValorMensal)) return 0;
                // Remove pontos de milhar e troca vírgula por ponto se necessário, ou usa cultura BR
                var cultura = new CultureInfo("pt-BR");
                if (decimal.TryParse(ValorMensal, NumberStyles.Currency, cultura, out decimal valor))
                    return valor;
                return 0;
            }
        }

        public decimal ValorContrato { get; set; } // Total calculado
        public string? Observacao { get; set; }
        public bool Ativo { get; set; } = true;

        [Display(Name = "Orçamento")]
        [Required(ErrorMessage = "O contrato deve estar vinculado a um Orçamento.")]
        public int? OrcamentoId { get; set; }

        [Display(Name = "Naturezas do Contrato")]
        public List<ContratoNaturezaViewModel> Naturezas { get; set; } = new List<ContratoNaturezaViewModel>();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (DataFim < DataInicio)
                yield return new ValidationResult("Data final deve ser maior que inicial.", new[] { nameof(DataFim) });

            // Validação manual do valor (já que agora é string)
            if (ValorMensalDecimal <= 0)
            {
                yield return new ValidationResult("O valor mensal deve ser maior que zero.", new[] { nameof(ValorMensal) });
            }

            // Validação de Naturezas
            if (Naturezas == null || !Naturezas.Any())
            {
                yield return new ValidationResult("Adicione ao menos uma natureza.", new[] { nameof(Naturezas) });
            }
            else
            {
                var somaNaturezas = Naturezas.Where(n => n.NaturezaId > 0).Sum(n => n.Valor);
                
                // Compara usando a propriedade convertida
                if (Math.Abs(ValorMensalDecimal - somaNaturezas) > 0.05m)
                {
                    yield return new ValidationResult(
                        $"A soma das naturezas ({somaNaturezas:C}) difere do Valor Mensal ({ValorMensalDecimal:C}).",
                        new[] { "SomaNaturezas" });
                }
            }
        }
    }
}