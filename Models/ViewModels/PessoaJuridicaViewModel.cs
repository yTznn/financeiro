using System.ComponentModel.DataAnnotations;

namespace Financeiro.Models.ViewModels
{
    public class PessoaJuridicaViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "A Razão Social é obrigatória."), Display(Name = "Razão Social")]
        public string RazaoSocial { get; set; }

        [Required(ErrorMessage = "O Nome Fantasia é obrigatório."), Display(Name = "Nome Fantasia")]
        public string NomeFantasia { get; set; }

        [Required(ErrorMessage = "O CNPJ é obrigatório."), Display(Name = "CNPJ")]
        public string NumeroInscricao { get; set; }

        // ADICIONADO O '?' PARA TORNAR OPCIONAL
        [EmailAddress(ErrorMessage = "E-mail inválido."), Display(Name = "E-mail")]
        public string? Email { get; set; }

        // ADICIONADO O '?' PARA TORNAR OPCIONAL
        [Phone, Display(Name = "Telefone")]
        public string? Telefone { get; set; }

        [Display(Name = "Ativo")]
        public bool SituacaoAtiva { get; set; } = true;
    }
}