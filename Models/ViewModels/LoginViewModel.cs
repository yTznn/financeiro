using Microsoft.AspNetCore.Mvc.Rendering;
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

        [Display(Name = "Entidade")]
        public int? EntidadeId { get; set; }

        public List<SelectListItem> EntidadesDisponiveis { get; set; } = new();

        public string? MensagemErro { get; set; }
    }
}