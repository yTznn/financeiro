using System.ComponentModel.DataAnnotations;

namespace Financeiro.Models.ViewModels
{
    public class EntidadeViewModel
    {
        public int? Id { get; set; }

        [Display(Name = "Nome da Entidade"), Required(ErrorMessage = "O nome é obrigatório"), StringLength(200)]
        public string Nome { get; set; } = null!;

        [Display(Name = "Sigla"), Required(ErrorMessage = "A sigla é obrigatória"), StringLength(20)]
        public string Sigla { get; set; } = null!;

        [Display(Name = "CNPJ"), Required(ErrorMessage = "O CNPJ é obrigatório"), StringLength(18)]
        public string Cnpj { get; set; } = null!;

        [Display(Name = "Endereço")]
        public int? EnderecoId { get; set; }

        [Display(Name = "Ativo")]
        public bool Ativo { get; set; } = true;

        [Display(Name = "Disponível para vínculo com usuário")]
        public bool VinculaUsuario { get; set; } = true;

        [Display(Name = "Observações")]
        public string? Observacao { get; set; }

        // =================================================================
        // DADOS BANCÁRIOS (Gerenciados via permissão ENTIDADE_CONTA_EDIT)
        // =================================================================
        
        public int? ContaBancariaId { get; set; } // Mantemos para referência oculta

        [Display(Name = "Banco")]
        [StringLength(100)]
        public string? Banco { get; set; }

        [Display(Name = "Agência")]
        [StringLength(20)]
        public string? Agencia { get; set; }

        [Display(Name = "Conta")]
        [StringLength(30)]
        public string? Conta { get; set; }

        [Display(Name = "Chave PIX")]
        [StringLength(120)]
        public string? ChavePix { get; set; }
    }
}