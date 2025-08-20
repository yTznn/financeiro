using System;

namespace Financeiro.Models.ViewModels
{
    public class CancelarAditivoViewModel
    {
        public int? VersaoId { get; set; }
        public int Versao { get; set; }
        public int ContratoId { get; set; }
        public string? ObjetoContrato { get; set; }
        public decimal ValorContrato { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime DataFim   { get; set; }
        public string Justificativa { get; set; } = string.Empty;
    }
}