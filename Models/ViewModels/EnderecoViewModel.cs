using System.ComponentModel.DataAnnotations;

namespace Financeiro.Models.ViewModels
{
    public class EnderecoViewModel
    {
        public int Id { get; set; }                // 0 = novo | >0 = edição
        public int PessoaJuridicaId { get; set; }  // obrigatório p/ vínculo

        [Required, Display(Name = "Logradouro")]
        public string Logradouro { get; set; }

        [Required, Display(Name = "Número")]
        public string Numero { get; set; }         // aceitará “SN”

        [Display(Name = "Complemento")]
        public string Complemento { get; set; }

        [Required, Display(Name = "CEP")]
        [RegularExpression(@"\d{8}", ErrorMessage = "CEP deve ter 8 dígitos.")]
        public string Cep { get; set; }

        [Required, Display(Name = "Bairro/Distrito")]
        public string Bairro { get; set; }

        [Required, Display(Name = "Município")]
        public string Municipio { get; set; }

        [Required, Display(Name = "UF")]
        [StringLength(2, MinimumLength = 2, ErrorMessage = "UF deve ter 2 letras.")]
        public string Uf { get; set; }
    }
}