using System;
using System.ComponentModel.DataAnnotations;

namespace Financeiro.Models.ViewModels
{
    public class PessoaFisicaViewModel
    {
        public int Id { get; set; }

        [Required, Display(Name = "Nome")]
        public string Nome { get; set; }

        [Required, Display(Name = "Sobrenome")]
        public string Sobrenome { get; set; }

        [Required, Display(Name = "CPF")]
        [RegularExpression(@"\d{11}", ErrorMessage = "CPF deve conter 11 dígitos.")]
        public string Cpf { get; set; }

        [Required, Display(Name = "Data de Nascimento")]
        [DataType(DataType.Date)]
        public DateTime DataNascimento { get; set; }

        [EmailAddress, Display(Name = "E-mail")]
        public string Email { get; set; }

        [Phone, Display(Name = "Telefone")]
        public string Telefone { get; set; }

        [Display(Name = "Ativo")]
        public bool SituacaoAtiva { get; set; } = true;
    }
}