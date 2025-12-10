using System;
using System.ComponentModel.DataAnnotations;

namespace Financeiro.Models.ViewModels
{
    public class InstrumentoResumoViewModel
    {
        [Display(Name = "ID")]
        public int InstrumentoId { get; set; }

        [Display(Name = "Número")]
        public string? Numero { get; set; }

        [Display(Name = "Descrição")]
        public string? Descricao { get; set; }

        // --- ADICIONADO PARA SUPORTAR O DESIGN DO CARD ---
        [Display(Name = "Vigente?")]
        public bool Vigente { get; set; }
        // -------------------------------------------------

        [Display(Name = "Início da Vigência")]
        [DataType(DataType.Date)]
        public DateTime VigenciaInicio { get; set; }

        [Display(Name = "Fim da Vigência (Atual)")]
        [DataType(DataType.Date)]
        public DateTime VigenciaFimAtual { get; set; }

        [Display(Name = "Meses Vigentes (Atuais)")]
        public int MesesVigentesAtuais { get; set; }

        [Display(Name = "Valor Total (Atual)")]
        [DataType(DataType.Currency)]
        public decimal ValorTotalAtual { get; set; }

        [Display(Name = "Valor Mensal (Atual)")]
        [DataType(DataType.Currency)]
        public decimal ValorMensalAtual { get; set; }

        [Display(Name = "Saldo (Atual)")]
        [DataType(DataType.Currency)]
        public decimal SaldoAtual { get; set; }

        [DataType(DataType.Currency)]
        public decimal TotalLancadoNoMes { get; set; }

        [DataType(DataType.Currency)]
        public decimal SaldoDoMesAtual { get; set; }
    }
}