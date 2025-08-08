using System.ComponentModel.DataAnnotations;

namespace Financeiro.Models.ViewModels
{
    public class EntidadeViewModel
    {
        public int? Id { get; set; }  // null para Create

        [Display(Name = "Nome da Entidade"), Required, StringLength(200)]
        public string Nome { get; set; } = null!;

        [Display(Name = "Sigla"), Required, StringLength(20)]
        public string Sigla { get; set; } = null!;

        [Display(Name = "CNPJ"), Required, StringLength(18)]
        public string Cnpj { get; set; } = null!;   // máscara 00.000.000/0000-00 no front

        [Display(Name = "Conta Bancária")]
        public int? ContaBancariaId { get; set; }

        [Display(Name = "Endereço")]
        public int? EnderecoId { get; set; }

        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;

        [Display(Name = "Disponível para vínculo com usuário")]
        public bool VinculaUsuario { get; set; } = true;

        [Display(Name = "Observações")]
        public string? Observacao { get; set; }
    }
}