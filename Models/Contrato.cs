using System;

namespace Financeiro.Models
{
    /// <summary>
    /// Representa a tabela Contrato no banco de dados.
    /// </summary>
    public class Contrato
    {
        public int Id { get; set; }
        public int? PessoaJuridicaId { get; set; }
        public int? PessoaFisicaId { get; set; }
        public int NumeroContrato { get; set; }
        public int AnoContrato { get; set; }
        public string ObjetoContrato { get; set; } = string.Empty;
        public DateTime? DataAssinatura { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public decimal ValorContrato { get; set; }
        public string? Observacao { get; set; }
        public bool Ativo { get; set; }
    }
}