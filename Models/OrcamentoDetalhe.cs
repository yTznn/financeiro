namespace Financeiro.Models
{
    public class OrcamentoDetalhe
    {
        public int Id { get; set; }
        public int OrcamentoId { get; set; }
        public int? ParentId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public decimal ValorPrevisto { get; set; }
        public bool PermiteLancamento { get; set; }

        // Propriedades de navegação (opcionais, mas úteis para EF Core, se usar)
        // public virtual Orcamento Orcamento { get; set; }
        // public virtual OrcamentoDetalhe Parent { get; set; }
        // public virtual ICollection<OrcamentoDetalhe> Children { get; set; }
    }
}