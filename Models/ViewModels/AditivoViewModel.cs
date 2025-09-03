using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Financeiro.Models;

namespace Financeiro.Models.ViewModels
{
    public class AditivoViewModel : IValidatableObject
    {
        [Required]
        public int TipoAcordoId { get; set; }

        [Required(ErrorMessage = "Selecione o tipo de aditivo.")]
        [Display(Name = "Tipo de Aditivo")]
        public TipoAditivo TipoAditivo { get; set; }

        [Display(Name = "Valor")] // Nome alterado para refletir que é o valor do aditivo
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
                        "O Valor é obrigatório para esse tipo de aditivo.",
                        new[] { nameof(NovoValor) });
                }
                // --- ALTERAÇÃO PRINCIPAL AQUI ---
                // A validação agora apenas garante que o valor inserido seja sempre positivo.
                // A responsabilidade de negativar o valor (para supressão) é do Controller.
                else if (NovoValor.Value <= 0)
                {
                     yield return new ValidationResult(
                        "O valor do aditivo deve ser maior que zero.",
                        new[] { nameof(NovoValor) });
                }
            }

            if (alteraPrazo && (NovaDataInicio is null || NovaDataFim is null))
                yield return new ValidationResult(
                    "Nova Data Início e Nova Data Fim são obrigatórias quando há alteração de prazo.",
                    new[] { nameof(NovaDataInicio), nameof(NovaDataFim) });

            if (NovaDataInicio is not null && NovaDataFim is not null &&
                NovaDataFim < NovaDataInicio)
            {
                yield return new ValidationResult(
                    "Nova Data Fim deve ser maior ou igual à Nova Data Início.",
                    new[] { nameof(NovaDataFim) });
            }
        }
    }
}