using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq; // Necessário para o .Sum()

namespace Financeiro.Models.ViewModels
{
    /// <summary>
    /// Dados que o usuário informa ao registrar um aditivo de Contrato.
    /// Suporta edição item a item e alteração completa de vigência (Início e Fim).
    /// </summary>
    public class AditivoContratoViewModel
    {
        [Required]
        public int ContratoId { get; set; }

        [Required(ErrorMessage = "Selecione o tipo de aditivo.")]
        [Display(Name = "Tipo de Aditivo")]
        public TipoAditivo TipoAditivo { get; set; }

        [Required(ErrorMessage = "A data de início do termo aditivo é obrigatória.")]
        [Display(Name = "Data de Assinatura/Início do Termo")]
        [DataType(DataType.Date)]
        public DateTime? DataInicioAditivo { get; set; }

        // --- CAMPOS DE VIGÊNCIA (INÍCIO E FIM) ---
        // Permite alterar o "nascimento" do contrato caso necessário
        [Display(Name = "Nova Data Início da Vigência")]
        [DataType(DataType.Date)]
        public DateTime? NovaDataInicio { get; set; }

        [Display(Name = "Nova Data Fim da Vigência")]
        [DataType(DataType.Date)]
        public DateTime? NovaDataFim { get; set; }

        [Display(Name = "Novo Objeto do Contrato")]
        public string? NovoObjeto { get; set; }

        [Required(ErrorMessage = "A justificativa do aditivo é obrigatória.")]
        [Display(Name = "Justificativa / Observação")]
        public string Justificativa { get; set; } = string.Empty;

        // Mantemos para compatibilidade, mapeando para Justificativa
        public string? Observacao 
        { 
            get => Justificativa; 
            set => Justificativa = value ?? string.Empty; 
        }

        // --- GRID DE ITENS ---
        // Transporta os valores editados da tela para o Controller
        public List<ContratoItemViewModel> Itens { get; set; } = new List<ContratoItemViewModel>();

        // Propriedade auxiliar para validações de backend
        public decimal NovoValorTotal => Itens?.Sum(i => i.Valor) ?? 0;
    }
}