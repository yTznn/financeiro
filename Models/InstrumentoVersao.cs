using System;

namespace Financeiro.Models
{
    /// <summary>
    /// Cada linha representa uma versão do Instrumento.
    /// A versão vigente é aquela com VigenciaFim = null.
    /// </summary>
    public class InstrumentoVersao
    {
        public int      Id                { get; set; }
        public int      InstrumentoId     { get; set; }   // FK para Instrumento
        public int      Versao            { get; set; }   // 1 = original, 2 = 1º aditivo…
        public DateTime VigenciaInicio    { get; set; }
        public DateTime? VigenciaFim      { get; set; }   // null = versão vigente
        public decimal  Valor             { get; set; }
        public string   Objeto            { get; set; } = string.Empty;
        public TipoAditivo? TipoAditivo   { get; set; }   // null para versão 1
        public string?  Observacao        { get; set; }
        public DateTime? DataAssinatura   { get; set; }
        public DateTime DataRegistro      { get; set; } = DateTime.Now;
    }
}