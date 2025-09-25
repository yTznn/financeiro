using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Financeiro.Models.ViewModels
{
    /// <summary>
    /// Representa os dados do formulário de criação/edição de Contrato.
    /// </summary>
    public class ContratoViewModel : IValidatableObject
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "É obrigatório selecionar um fornecedor.")]
        [Display(Name = "Fornecedor")]
        public string FornecedorIdCompleto { get; set; } // Formato "PF-123" ou "PJ-456"

        [Required(ErrorMessage = "O número do contrato é obrigatório.")]
        [Display(Name = "Número do Contrato")]
        public int NumeroContrato { get; set; }

        [Required(ErrorMessage = "O ano do contrato é obrigatório.")]
        [Range(2000, 2100, ErrorMessage = "Ano inválido.")]
        [Display(Name = "Ano do Contrato")]
        public int AnoContrato { get; set; } = DateTime.Now.Year;

        [Required(ErrorMessage = "O objeto do contrato é obrigatório.")]
        [Display(Name = "Objeto do Contrato")]
        public string ObjetoContrato { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        [Display(Name = "Data de Assinatura")]
        public DateTime? DataAssinatura { get; set; }

        [Required(ErrorMessage = "A data de início da vigência é obrigatória.")]
        [DataType(DataType.Date)]
        [Display(Name = "Início da Vigência")]
        public DateTime DataInicio { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "A data de fim da vigência é obrigatória.")]
        [DataType(DataType.Date)]
        [Display(Name = "Fim da Vigência")]
        public DateTime DataFim { get; set; } = DateTime.Today.AddYears(1);

        // ✅ NOVO CAMPO PARA O FORMULÁRIO
        [Required(ErrorMessage = "O valor mensal é obrigatório.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero.")]
        [Display(Name = "Valor Mensal")]
        public decimal ValorMensal { get; set; }

        // Este campo agora será calculado e não mais digitado pelo usuário.
        [Display(Name = "Valor Total do Contrato")]
        public decimal ValorContrato { get; set; }

        [Display(Name = "Observação")]
        public string? Observacao { get; set; }

        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;

        [Required(ErrorMessage = "Selecione ao menos uma natureza para o contrato.")]
        [Display(Name = "Naturezas do Contrato")]
        public List<int> NaturezasIds { get; set; } = new List<int>();
        [Display(Name = "Orçamento")]
        public int? OrcamentoId { get; set; }

        // Validação customizada para garantir que a data final é maior que a inicial
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (DataFim < DataInicio)
            {
                yield return new ValidationResult(
                    "A data de fim da vigência deve ser maior ou igual à data de início.",
                    new[] { nameof(DataFim) });
            }
        }
    }
}