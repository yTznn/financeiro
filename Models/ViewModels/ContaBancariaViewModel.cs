using System.ComponentModel.DataAnnotations;

namespace Financeiro.Models.ViewModels
{
    public class ContaBancariaViewModel
    {
        public int Id { get; set; }                // 0 = nova conta | >0 = edição

        // Identificador da pessoa (usaremos um dos dois)
        public int? PessoaJuridicaId { get; set; }
        public int? PessoaFisicaId   { get; set; }

        [Required, Display(Name = "Banco")]
        public string Banco { get; set; }

        [Required, Display(Name = "Agência")]
        public string Agencia { get; set; }

        [Required, Display(Name = "Conta")]
        public string Conta { get; set; }

        [Display(Name = "Chave Pix")]
        public string ChavePix { get; set; }
    }
}