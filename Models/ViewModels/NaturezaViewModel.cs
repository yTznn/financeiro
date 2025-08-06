using System.ComponentModel.DataAnnotations;

namespace Financeiro.Models.ViewModels
{
    public class NaturezaViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Informe o nome da natureza.")]
        [Display(Name = "Nome")]
        public string Nome { get; set; } = string.Empty;

        [Display(Name = "Natureza Médica?")]
        public bool NaturezaMedica { get; set; }

        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;
    }
}