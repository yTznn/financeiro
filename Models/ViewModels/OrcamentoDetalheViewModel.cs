// Este VM representa uma linha na árvore do modal
namespace Financeiro.Models.ViewModels
{
    public class OrcamentoDetalheViewModel
    {
        public int? Id { get; set; } // Null para novos itens
        public int? ParentId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public decimal ValorPrevisto { get; set; }
        public bool PermiteLancamento { get; set; }
        // Para reconstruir a árvore no front-end
        public List<OrcamentoDetalheViewModel> Filhos { get; set; } = new();
    }
}