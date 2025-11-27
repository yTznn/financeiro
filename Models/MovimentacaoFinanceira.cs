using System;
using System.Collections.Generic;

namespace Financeiro.Models
{
    public class MovimentacaoFinanceira
    {
        public int Id { get; set; }
        public DateTime DataMovimentacao { get; set; }
        public DateTime DataCriacao { get; set; }
        public string? FornecedorIdCompleto { get; set; } // "PJ-1"
        public decimal ValorTotal { get; set; }
        public string? Historico { get; set; }
        public bool Ativo { get; set; }

        // Para exibição na lista
        public string NomeFornecedor { get; set; } 
    }
}