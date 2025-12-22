using System;
using System.ComponentModel.DataAnnotations;

namespace Financeiro.Models.ViewModels
{
    public class PessoaFisicaViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O Nome é obrigatório."), Display(Name = "Nome")]
        public string Nome { get; set; }

        [Required(ErrorMessage = "O Sobrenome é obrigatório."), Display(Name = "Sobrenome")]
        public string Sobrenome { get; set; }

        [Required(ErrorMessage = "O CPF é obrigatório."), Display(Name = "CPF")]
        public string Cpf { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Data de Nascimento")]
        public DateTime? DataNascimento { get; set; }

        // ADICIONADO O '?' PARA TORNAR OPCIONAL
        [EmailAddress(ErrorMessage = "E-mail inválido."), Display(Name = "E-mail")]
        public string? Email { get; set; }

        // MANTIDO O '?' (Já estava correto)
        [Phone, Display(Name = "Telefone")]
        public string? Telefone { get; set; }

        [Display(Name = "Ativo")]
        public bool? SituacaoAtiva { get; set; } = true;
    }
}