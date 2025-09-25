namespace Financeiro.Models
{
    public class Orcamento
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public DateTime VigenciaInicio { get; set; }
        public DateTime VigenciaFim { get; set; }
        public decimal ValorPrevistoTotal { get; set; }
        public bool Ativo { get; set; }
        public DateTime DataCriacao { get; set; }
        public string? Observacao { get; set; } 
    }
}