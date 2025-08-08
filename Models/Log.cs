using System;

namespace Financeiro.Models
{
    public class Log
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public int EntidadeId { get; set; }
        public string Acao { get; set; } = string.Empty;
        public string Tabela { get; set; } = string.Empty;
        public DateTime DataHora { get; set; }
        public string? ValoresAnteriores { get; set; }
        public string? ValoresNovos { get; set; }
    }
}