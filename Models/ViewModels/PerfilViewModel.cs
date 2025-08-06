namespace Financeiro.Models.ViewModels
{
    public class PerfilViewModel
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public bool Ativo { get; set; } = true;
    }
}