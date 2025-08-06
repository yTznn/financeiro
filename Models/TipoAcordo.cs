using System;

namespace Financeiro.Models
{
    public class TipoAcordo
    {
        public int Id { get; set; }
        public string Numero { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public string Objeto { get; set; } = string.Empty;
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public bool Ativo { get; set; }
        public string? Observacao { get; set; }
        public DateTime? DataAssinatura { get; set; }
    }
}