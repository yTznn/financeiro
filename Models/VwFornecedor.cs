namespace Financeiro.Models
{
    /// <summary>
    /// Representa a View Vw_Fornecedores para busca unificada.
    /// </summary>
    public class VwFornecedor
    {
        public int FornecedorId { get; set; }
        public string Nome { get; set; }
        public string? NomeFantasia { get; set; }
        public string Documento { get; set; }
        public string Tipo { get; set; }
        public bool SituacaoAtiva { get; set; }
        public string? Email { get; set; }
        public string? Telefone { get; set; }
    }
}