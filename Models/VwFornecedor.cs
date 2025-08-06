namespace Financeiro.Models
{
    /// <summary>
    /// Representa a View Vw_Fornecedores para busca unificada.
    /// </summary>
    public class VwFornecedor
    {
        public int FornecedorId { get; set; }
        public string Tipo { get; set; } = string.Empty; // "PJ" ou "PF"
        public string Nome { get; set; } = string.Empty;
        public string Documento { get; set; } = string.Empty;
    }
}