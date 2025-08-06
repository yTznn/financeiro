using System;
using System.Linq;
using Financeiro.Models.ViewModels;

namespace Financeiro.Validacoes
{
    public class PessoaFisicaValidacoes
    {
        public ResultadoValidacao Validar(PessoaFisicaViewModel vm)
        {
            var r = new ResultadoValidacao();

            if (string.IsNullOrWhiteSpace(vm.Nome))
                r.Erros.Add("Nome é obrigatório.");

            if (string.IsNullOrWhiteSpace(vm.Sobrenome))
                r.Erros.Add("Sobrenome é obrigatório.");

            if (!CpfEhValido(vm.Cpf))
                r.Erros.Add("CPF inválido ou em branco.");

            if (vm.DataNascimento == default || vm.DataNascimento > DateTime.Today)
                r.Erros.Add("Data de nascimento inválida.");

            if (!EhEmailValido(vm.Email))
                r.Erros.Add("E-mail inválido ou vazio.");

            if (vm.Telefone == null || vm.Telefone.Count(char.IsDigit) < 8)
                r.Erros.Add("Telefone precisa ter ao menos 8 dígitos.");

            return r;
        }

        private bool EhEmailValido(string email)
            => !string.IsNullOrWhiteSpace(email) &&
               System.Net.Mail.MailAddress.TryCreate(email, out _);

        private bool CpfEhValido(string cpfRaw)
        {
            if (string.IsNullOrWhiteSpace(cpfRaw)) return false;
            var cpf = new string(cpfRaw.Where(char.IsDigit).ToArray());
            return cpf.Length == 11;   // regra simplificada
        }
    }
}