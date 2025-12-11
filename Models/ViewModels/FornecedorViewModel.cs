namespace Financeiro.Models.ViewModels
{
    public class FornecedorViewModel
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;     // Nome (PF) ou Razão Social (PJ)
        public string Documento { get; set; } = string.Empty; // CPF ou CNPJ
        public string TipoPessoa { get; set; } = string.Empty; // "PF" ou "PJ"
        public string Email { get; set; } = string.Empty;
        public string Telefone { get; set; } = string.Empty;
    }
}