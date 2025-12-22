using System.ComponentModel.DataAnnotations;

namespace Financeiro.Models.ViewModels
{
    /// <summary>
    /// ViewModel para criação/edição e listagem de contas bancárias.
    /// Observações:
    /// - Id            => Id da TABELA ContaBancaria.
    /// - VinculoId     => Id da TABELA PessoaConta (vínculo PF/PJ com a conta).
    /// - IsPrincipal   => marcado no vínculo (PessoaConta.IsPrincipal).
    /// - PessoaFisicaId/PessoaJuridicaId => um OU outro (quem é o dono do vínculo).
    /// </summary>
    public class ContaBancariaViewModel
    {
        /// <summary>Id da conta em ContaBancaria (0 = nova, >0 = edição)</summary>
        public int Id { get; set; }

        /// <summary>Id do vínculo em PessoaConta (nulo quando ainda não existe vínculo)</summary>
        public int? VinculoId { get; set; }

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
        public string? ChavePix { get; set; } // <--- Interrogação aqui!

        /// <summary>Se verdadeiro, define este vínculo como principal (desmarca os demais do mesmo dono)</summary>
        [Display(Name = "Principal?")]
        public bool IsPrincipal { get; set; }
    }
}