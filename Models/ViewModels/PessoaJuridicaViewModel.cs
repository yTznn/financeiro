using System.ComponentModel.DataAnnotations;

namespace Financeiro.Models.ViewModels
{
    public class PessoaJuridicaViewModel
    {
        public int Id { get; set; }   // ← NOVO

        [Required, Display(Name = "Razão Social")]
        public string RazaoSocial { get; set; }

        [Required, Display(Name = "Nome Fantasia")]
        public string NomeFantasia { get; set; }

        [Required, Display(Name = "CNPJ")]
        [RegularExpression(@"\d{14}", ErrorMessage = "CNPJ deve conter 14 dígitos.")]
        public string NumeroInscricao { get; set; }

        [EmailAddress, Display(Name = "E-mail")]
        public string Email { get; set; }

        [Phone, Display(Name = "Telefone")]
        public string Telefone { get; set; }

        [Display(Name = "Ativo")]
        public bool SituacaoAtiva { get; set; } = true;
    }
}