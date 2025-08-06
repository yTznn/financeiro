namespace Financeiro.Models.Dto
{
    public class UsuarioListagemTemp
    {
        public int Id { get; set; }
        public string NameSkip { get; set; }
        public string EmailCriptografado { get; set; }
        public string? NomePessoaFisica { get; set; }
        public string? HashImagem { get; set; }
        public bool Ativo { get; set; }
    }
}