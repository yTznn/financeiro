using System.ComponentModel.DataAnnotations;

namespace Financeiro.Models.ViewModels
{
    /// <summary>
    /// ViewModel de Endereço reutilizável para Pessoa Jurídica e Pessoa Física.
    /// Suporta múltiplos endereços e marcação de "principal".
    /// </summary>
    public class EnderecoViewModel
    {
        /// <summary>0 = novo | > 0 = edição</summary>
        public int Id { get; set; }

        /// <summary>
        /// Identificador da Pessoa Jurídica (mantido por compatibilidade com o fluxo legado).
        /// Quando usado para PF, deixe 0.
        /// </summary>
        public int PessoaJuridicaId { get; set; }

        /// <summary>
        /// Identificador da Pessoa Física (para reaproveitar o mesmo form).
        /// Quando usado para PJ, deixe null.
        /// </summary>
        public int? PessoaFisicaId { get; set; }

        [Required, Display(Name = "Logradouro")]
        public string Logradouro { get; set; }

        [Required, Display(Name = "Número")]
        public string Numero { get; set; } // aceita "SN"

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

        /// <summary>
        /// Checkbox do formulário de criação para marcar este endereço como principal.
        /// Só faz sentido quando Id == 0 (novo). No controller, se não houver principal ainda,
        /// este endereço será marcado como principal mesmo que o usuário não marque.
        /// </summary>
        [Display(Name = "Definir como principal")]
        public bool DefinirPrincipal { get; set; }

        /// <summary>
        /// Indica se o endereço atual é o principal (usado para exibição na edição).
        /// Preenchido pelo controller no GET de edição.
        /// </summary>
        public bool EhPrincipal { get; set; }
    }
}