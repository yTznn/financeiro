using System;

namespace Financeiro.Models
{
    public class MovimentacaoFinanceira
    {
        public int Id { get; set; }
        public DateTime DataMovimentacao { get; set; }
        public DateTime DataCriacao { get; set; }
        public string? FornecedorIdCompleto { get; set; }
        public decimal ValorTotal { get; set; }
        public string? Historico { get; set; }
        public bool Ativo { get; set; }

        // --- NOVOS CAMPOS ---
        public DateTime? DataReferenciaInicio { get; set; }
        public DateTime? DataReferenciaFim { get; set; }
        public bool EhLancamentoAvulso { get; set; }
        public string? JustificativaAvulso { get; set; }

        // Auxiliares
        public string NomeFornecedor { get; set; } 
    }
}