using System.ComponentModel.DataAnnotations;

namespace Financeiro.Models
{
    public class Perfil
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome do perfil é obrigatório.")]
        [StringLength(100)]
        public string Nome { get; set; } = string.Empty;

        public bool Ativo { get; set; } = true;
    }
}