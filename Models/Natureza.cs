namespace Financeiro.Models
{
    /// <summary>
    /// Natureza (ou especialidade) de contrato/serviço.
    /// </summary>
    public class Natureza
    {
        public int  Id             { get; set; }
        public string Nome         { get; set; } = string.Empty;
        public bool NaturezaMedica { get; set; }   // true = médica
        public bool Ativo          { get; set; }
    }
}