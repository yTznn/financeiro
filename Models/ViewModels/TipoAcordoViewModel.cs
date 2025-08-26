using System;
using System.ComponentModel.DataAnnotations;

namespace Financeiro.Models.ViewModels
{
    public class TipoAcordoViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Informe o número do acordo.")]
        [StringLength(100, ErrorMessage = "Número muito longo (máx. {1} caracteres).")]
        public string Numero { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe o valor.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Valor deve ser maior que zero.")]
        public decimal Valor { get; set; }

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

        [Display(Name = "Está ativo?")]
        public bool Ativo { get; set; }

        [Display(Name = "Observação")]
        public string? Observacao { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Data de Assinatura")]
        public DateTime? DataAssinatura { get; set; }

        // ➜ NOVO (para o select de Entidade)
        [Required(ErrorMessage = "Selecione a Entidade.")]
        [Display(Name = "Entidade")]
        public int EntidadeId { get; set; }
    }
}