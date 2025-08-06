using System.ComponentModel.DataAnnotations;

namespace Financeiro.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Informe seu e-mail ou NameSkip.")]
        [Display(Name = "E-mail ou NameSkip")]
        public string Login { get; set; }

        [Required(ErrorMessage = "Informe a senha.")]
        [DataType(DataType.Password)]
        public string Senha { get; set; }

        public string? MensagemErro { get; set; }
    }
}