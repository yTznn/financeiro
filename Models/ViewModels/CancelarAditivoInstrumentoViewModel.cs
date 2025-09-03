namespace Financeiro.Models.ViewModels
{
    public class CancelarAditivoInstrumentoViewModel
    {
        public int InstrumentoId { get; set; }
        public int Versao { get; set; }
        public string Justificativa { get; set; } = "";
    }
}