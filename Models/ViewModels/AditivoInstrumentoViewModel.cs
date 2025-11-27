using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Financeiro.Models;

namespace Financeiro.Models.ViewModels
{
    public class AditivoInstrumentoViewModel : IValidatableObject
    {
        [Required]
        [Display(Name = "Instrumento")]
        public int InstrumentoId { get; set; }

        [Required(ErrorMessage = "Selecione o tipo de aditivo.")]
        [Display(Name = "Tipo de Aditivo")]
        public TipoAditivo TipoAditivo { get; set; }

        [Display(Name = "Valor")]
        public decimal? NovoValor { get; set; }

        [Display(Name = "Nova Data Início")]
        [DataType(DataType.Date)]
        public DateTime? NovaDataInicio { get; set; }

        [Display(Name = "Nova Data Fim")]
        [DataType(DataType.Date)]
        public DateTime? NovaDataFim { get; set; }

        [Display(Name = "Observação")]
        public string? Observacao { get; set; }

        [Display(Name = "Data de Assinatura")]
        [DataType(DataType.Date)]
        public DateTime? DataAssinatura { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext context)
        {
            bool alteraValor = TipoAditivo is TipoAditivo.Acrescimo
                             or TipoAditivo.Supressao
                             or TipoAditivo.PrazoAcrescimo
                             or TipoAditivo.PrazoSupressao;

            bool alteraPrazo = TipoAditivo is TipoAditivo.Prazo
                             or TipoAditivo.PrazoAcrescimo
                             or TipoAditivo.PrazoSupressao;

            if (alteraValor)
            {
                if (NovoValor is null)
                {
                    yield return new ValidationResult(
                        "O valor é obrigatório para esse tipo de aditivo.",
                        new[] { nameof(NovoValor) });
                }
                else if (NovoValor.Value <= 0)
                {
                    // Supressão negativa será tratada na lógica do serviço/controller.
                    yield return new ValidationResult(
                        "O valor do aditivo deve ser maior que zero.",
                        new[] { nameof(NovoValor) });
                }
            }

            if (alteraPrazo && (NovaDataInicio is null || NovaDataFim is null))
            {
                yield return new ValidationResult(
                    "Nova Data Início e Nova Data Fim são obrigatórias quando há alteração de prazo.",
                    new[] { nameof(NovaDataInicio), nameof(NovaDataFim) });
            }

            if (NovaDataInicio is not null && NovaDataFim is not null &&
                NovaDataFim < NovaDataInicio)
            {
                yield return new ValidationResult(
                    "Nova Data Fim deve ser maior ou igual à Nova Data Início.",
                    new[] { nameof(NovaDataFim) });
            }
        }
        [Display(Name = "O valor informado é mensal?")]
        public bool EhValorMensal { get; set; }
    }
}