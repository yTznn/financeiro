using System;
using System.Collections.Generic;

namespace Financeiro.Models
{
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

        // REMOVIDOS: OrcamentoId e OrcamentoDetalheId (não existem mais na tabela Contrato)
        // O vínculo agora é via tabela filha.

        // Propriedade de Navegação (Para preencher via Dapper Multi-Map se necessário)
        public List<ContratoItem> Itens { get; set; } = new List<ContratoItem>();
    }
}