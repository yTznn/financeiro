using System.ComponentModel.DataAnnotations;

namespace Financeiro.Models
{
    public class Entidade
    {
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string Nome { get; set; } = null!;

        [Required, StringLength(20)]
        public string Sigla { get; set; } = null!;

        [Required, StringLength(14, MinimumLength = 14, ErrorMessage = "CNPJ deve conter 14 dígitos numéricos.")]
        [RegularExpression(@"^\d{14}$", ErrorMessage = "Informe apenas números no CNPJ.")]
        public string Cnpj { get; set; } = null!;

        public int? ContaBancariaId { get; set; }
        public int? EnderecoId { get; set; }

        public bool Ativo { get; set; } = true;
        public bool VinculaUsuario { get; set; } = true;

        [StringLength(int.MaxValue)]
        public string? Observacao { get; set; }
    }
}