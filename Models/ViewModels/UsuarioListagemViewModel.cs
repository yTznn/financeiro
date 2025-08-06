namespace Financeiro.Models.ViewModels
{
    public class UsuarioListagemViewModel
    {
        public int Id { get; set; }

        public string NameSkip { get; set; }
        public string EmailDescriptografado { get; set; }

        public string Email { get; set; }

        public string? NomePessoaFisica { get; set; }

        public string? HashImagem { get; set; }

        public bool Ativo { get; set; }
    }
}