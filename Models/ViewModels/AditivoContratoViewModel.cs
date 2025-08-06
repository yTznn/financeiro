using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Financeiro.Models;

namespace Financeiro.Models.ViewModels
{
    /// <summary>
    /// Dados que o usuário informa ao registrar um aditivo de Contrato.
    /// </summary>
    public class AditivoContratoViewModel : IValidatableObject
    {
        [Required]
        public int ContratoId { get; set; }

        [Required(ErrorMessage = "Selecione o tipo de aditivo.")]
        [Display(Name = "Tipo de Aditivo")]
        public TipoAditivo TipoAditivo { get; set; }
        
        // ✅ ADICIONE ESTAS 3 LINHAS
        [Required(ErrorMessage = "A data de início do aditivo é obrigatória.")]
        [Display(Name = "Data de Início do Aditivo")]
        [DataType(DataType.Date)]
        public DateTime? DataInicioAditivo { get; set; }

        // Campos que podem mudar
        [Display(Name = "Novo Valor do Contrato")]
        public decimal? NovoValor { get; set; }

        [Display(Name = "Nova Data Fim da Vigência")]
        [DataType(DataType.Date)]
        public DateTime? NovaDataFim { get; set; }

        [Display(Name = "Novo Objeto do Contrato")]
        public string? NovoObjeto { get; set; }

        // Metadados
        [Display(Name = "Observação")]
        public string? Observacao { get; set; }

        // Validação condicional para garantir que os campos corretos sejam preenchidos
        public IEnumerable<ValidationResult> Validate(ValidationContext context)
        {
            bool alteraValor = TipoAditivo is TipoAditivo.Acrescimo or TipoAditivo.Supressao or TipoAditivo.PrazoAcrescimo or TipoAditivo.PrazoSupressao;
            bool alteraPrazo = TipoAditivo is TipoAditivo.Prazo or TipoAditivo.PrazoAcrescimo or TipoAditivo.PrazoSupressao;

            if (alteraValor && NovoValor is null)
            {
                yield return new ValidationResult(
                    "Novo Valor é obrigatório para este tipo de aditivo.",
                    new[] { nameof(NovoValor) });
            }

            if (alteraPrazo && NovaDataFim is null)
            {
                yield return new ValidationResult(
                    "Nova Data Fim é obrigatória quando há alteração de prazo.",
                    new[] { nameof(NovaDataFim) });
            }
        }
    }
}