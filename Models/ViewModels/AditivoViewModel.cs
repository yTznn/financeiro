using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Financeiro.Models;   // p/ usar o enum TipoAditivo

namespace Financeiro.Models.ViewModels
{
    /// <summary>
    /// Dados que o usuário informa ao registrar um aditivo.
    /// Validação condicional feita em IValidatableObject.
    /// </summary>
    public class AditivoViewModel : IValidatableObject
    {
        /* ---------- vínculo ---------- */
        [Required]
        public int TipoAcordoId { get; set; }  // acordo que será aditivado

        /* ---------- escolha do tipo ---------- */
        [Required(ErrorMessage = "Selecione o tipo de aditivo.")]
        [Display(Name = "Tipo de Aditivo")]
        public TipoAditivo TipoAditivo { get; set; }

        /* ---------- campos que podem mudar ---------- */
        [Display(Name = "Novo Valor")]
        public decimal? NovoValor { get; set; }          // obrigatório p/ acréscimo ou supressão

        [Display(Name = "Nova Data Início")]
        [DataType(DataType.Date)]
        public DateTime? NovaDataInicio { get; set; }    // obrigatório se alterar prazo

        [Display(Name = "Nova Data Fim")]
        [DataType(DataType.Date)]
        public DateTime? NovaDataFim { get; set; }       // obrigatório se alterar prazo

        /* ---------- metadados ---------- */
        [Display(Name = "Observação")]
        public string? Observacao { get; set; }

        [Display(Name = "Data de Assinatura")]
        [DataType(DataType.Date)]
        public DateTime? DataAssinatura { get; set; }

        /* ---------- validação condicional ---------- */
        public IEnumerable<ValidationResult> Validate(ValidationContext context)
        {
            bool alteraValor = TipoAditivo is TipoAditivo.Acrescimo
                                         or TipoAditivo.Supressao
                                         or TipoAditivo.PrazoAcrescimo
                                         or TipoAditivo.PrazoSupressao;

            bool alteraPrazo = TipoAditivo is TipoAditivo.Prazo
                                         or TipoAditivo.PrazoAcrescimo
                                         or TipoAditivo.PrazoSupressao;

            if (alteraValor && NovoValor is null)
                yield return new ValidationResult(
                    "Novo Valor é obrigatório para esse tipo de aditivo.",
                    new[] { nameof(NovoValor) });

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