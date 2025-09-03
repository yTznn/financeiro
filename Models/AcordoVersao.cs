using System;
using Financeiro.Models.Dto;

namespace Financeiro.Models
{
    /// <summary>
    /// Cada linha representa a versão de um TipoAcordo.
    /// A versão atual é aquela com VigenciaFim = null.
    /// </summary>
    public class AcordoVersao
    {
        public int      Id               { get; set; }
        public int      TipoAcordoId     { get; set; }   // FK para TipoAcordo
        public int      Versao           { get; set; }   // 1 = original, 2 = 1º aditivo…
        public DateTime VigenciaInicio   { get; set; }
        public DateTime? VigenciaFim     { get; set; }   // null = versão vigente
        public decimal  Valor            { get; set; }
        public string   Objeto           { get; set; } = string.Empty;
        public TipoAditivo? TipoAditivo  { get; set; }   // null para versão 1
        public string?  Observacao       { get; set; }
        public DateTime? DataAssinatura  { get; set; }
        public DateTime DataRegistro     { get; set; } = DateTime.Now;
    }
}