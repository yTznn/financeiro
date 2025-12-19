using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;

namespace Financeiro.Models.ViewModels
{
    // 2. O ViewModel Principal
    public class ContratoViewModel : IValidatableObject
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "É obrigatório selecionar um fornecedor.")]
        [Display(Name = "Fornecedor")]
        public string FornecedorIdCompleto { get; set; }

        [Required(ErrorMessage = "O número do contrato é obrigatório.")]
        [Display(Name = "Número")]
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

        // O Valor Mensal agora é calculado e apenas informativo (ou usado para parcelas),
        // mas a fonte da verdade para a soma dos itens é o ValorContrato.
        [Display(Name = "Valor Mensal")]
        public string ValorMensal { get; set; } 

        public decimal ValorMensalDecimal
        {
            get
            {
                if (string.IsNullOrEmpty(ValorMensal)) return 0;
                var cultura = new CultureInfo("pt-BR");
                if (decimal.TryParse(ValorMensal, NumberStyles.Currency, cultura, out decimal valor))
                    return valor;
                return 0;
            }
        }

        // Este campo (ValorContrato) deve bater com a soma dos itens
        public decimal ValorContrato { get; set; } 
        
        public string? Observacao { get; set; }
        public bool Ativo { get; set; } = true;

        [Display(Name = "Orçamento Base")]
        [Required(ErrorMessage = "O orçamento principal é obrigatório.")]
        public int? OrcamentoId { get; set; }

        [Display(Name = "Itens do Contrato")]
        public List<ContratoItemViewModel> Itens { get; set; } = new List<ContratoItemViewModel>();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (DataFim < DataInicio)
                yield return new ValidationResult("Data final deve ser maior que inicial.", new[] { nameof(DataFim) });

            // Validação da Lista de Itens
            if (Itens == null || !Itens.Any())
            {
                yield return new ValidationResult("Adicione ao menos um item de orçamento ao contrato.", new[] { nameof(Itens) });
            }
            else
            {
                // A soma dos itens agora representa o TOTAL GLOBAL
                var somaItens = Itens.Sum(i => i.Valor);
                
                // CORREÇÃO AQUI:
                // Compara a soma dos itens com o ValorContrato (Total Global) e não com o Mensal.
                if (Math.Abs(ValorContrato - somaItens) > 0.05m)
                {
                    yield return new ValidationResult(
                        $"A soma dos itens ({somaItens:C2}) não fecha com o Valor Total Global ({ValorContrato:C2}). Diferença: {(ValorContrato - somaItens):C2}",
                        new[] { "SomaItens" });
                }
            }
        }
    }
}