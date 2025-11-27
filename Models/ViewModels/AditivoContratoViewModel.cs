using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization; // Necessário para a conversão de moeda
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

        [Required(ErrorMessage = "A data de início do aditivo é obrigatória.")]
        [Display(Name = "Data de Início do Aditivo")]
        [DataType(DataType.Date)]
        public DateTime? DataInicioAditivo { get; set; }

        // [NOVO] Checkbox para definir a lógica de cálculo
        [Display(Name = "O valor informado é mensal?")]
        public bool EhValorMensal { get; set; }

        // [ALTERADO] Mudamos para string para aceitar formatação PT-BR (vírgula) sem erro de validação imediato
        [Display(Name = "Valor do Aditivo")]
        public string? NovoValor { get; set; }

        // [NOVO] Propriedade auxiliar que converte o texto para Decimal (uso interno no Service)
        public decimal NovoValorDecimal
        {
            get
            {
                if (string.IsNullOrWhiteSpace(NovoValor)) return 0;
                
                // Tenta converter usando cultura Brasileira (vírgula)
                if (decimal.TryParse(NovoValor, NumberStyles.Currency, new CultureInfo("pt-BR"), out decimal valBr))
                    return valBr;
                
                // Fallback: Tenta converter formato internacional (ponto) caso venha limpo do JS
                if (decimal.TryParse(NovoValor, NumberStyles.Currency, CultureInfo.InvariantCulture, out decimal valInv))
                    return valInv;

                return 0;
            }
        }

        [Display(Name = "Nova Data Fim da Vigência")]
        [DataType(DataType.Date)]
        public DateTime? NovaDataFim { get; set; }

        [Display(Name = "Novo Objeto do Contrato")]
        public string? NovoObjeto { get; set; }

        [Display(Name = "Observação")]
        public string? Observacao { get; set; }

        // Validação condicional
        public IEnumerable<ValidationResult> Validate(ValidationContext context)
        {
            bool alteraValor = TipoAditivo is TipoAditivo.Acrescimo or TipoAditivo.Supressao or TipoAditivo.PrazoAcrescimo or TipoAditivo.PrazoSupressao;
            bool alteraPrazo = TipoAditivo is TipoAditivo.Prazo or TipoAditivo.PrazoAcrescimo or TipoAditivo.PrazoSupressao;

            if (alteraValor)
            {
                // Validamos o decimal convertido
                if (NovoValorDecimal <= 0)
                {
                    yield return new ValidationResult(
                        "Novo Valor é obrigatório e deve ser maior que zero para este tipo de aditivo.",
                        new[] { nameof(NovoValor) });
                }
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