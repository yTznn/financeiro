using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization; 
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

        [Display(Name = "O valor informado é mensal?")]
        public bool EhValorMensal { get; set; }

        [Display(Name = "Valor do Aditivo")]
        public string? NovoValor { get; set; }

        // Propriedade auxiliar que converte o texto para Decimal
        public decimal NovoValorDecimal
        {
            get
            {
                if (string.IsNullOrWhiteSpace(NovoValor)) return 0;
                
                // Tenta converter usando cultura Brasileira (vírgula)
                if (decimal.TryParse(NovoValor, NumberStyles.Currency, new CultureInfo("pt-BR"), out decimal valBr))
                    return valBr;
                
                // Fallback: Tenta converter formato internacional (ponto)
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

        // [CORREÇÃO] Adicionada a propriedade Justificativa explicitamente
        // A Observacao abaixo pode ser usada como complemento ou podemos usar apenas Justificativa
        [Required(ErrorMessage = "A justificativa do aditivo é obrigatória.")]
        [Display(Name = "Justificativa / Observação")]
        public string Justificativa { get; set; }

        // Mantemos Observacao apontando para Justificativa ou separada, 
        // mas para evitar quebra de código antigo que usa 'Observacao', deixamos ela aqui.
        // No service, mapeamos vm.Justificativa para o campo Observacao do banco.
        public string? Observacao 
        { 
            get => Justificativa; 
            set => Justificativa = value; 
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext context)
        {
            bool alteraValor = TipoAditivo == TipoAditivo.Acrescimo || 
                               TipoAditivo == TipoAditivo.Supressao || 
                               TipoAditivo == TipoAditivo.PrazoAcrescimo || 
                               TipoAditivo == TipoAditivo.PrazoSupressao;
            
            bool alteraPrazo = TipoAditivo == TipoAditivo.Prazo || 
                               TipoAditivo == TipoAditivo.PrazoAcrescimo || 
                               TipoAditivo == TipoAditivo.PrazoSupressao;

            if (alteraValor)
            {
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