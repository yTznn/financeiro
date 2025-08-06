// Local: Financeiro/Validacoes/PessoaFisicaValidacoes.cs

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

            // --- Limpa formatações antes de validar ---
            vm.Cpf      = ApenasNumeros(vm.Cpf);
            vm.Telefone = ApenasNumeros(vm.Telefone);

            // --- Validações dos campos ---
            if (string.IsNullOrWhiteSpace(vm.Nome))
                r.Erros.Add("O Nome é obrigatório.");

            if (string.IsNullOrWhiteSpace(vm.Sobrenome))
                r.Erros.Add("O Sobrenome é obrigatório.");

            if (!CpfEhValido(vm.Cpf))
                r.Erros.Add("O CPF informado é inválido.");

            if (vm.DataNascimento >= DateTime.Today)
                r.Erros.Add("A Data de Nascimento deve ser anterior à data de hoje.");

            if (vm.DataNascimento > DateTime.Today.AddYears(-18))
                r.Erros.Add("O cliente deve ser maior de 18 anos.");

            if (!EhEmailValido(vm.Email))
                r.Erros.Add("O E-mail informado é inválido ou está vazio.");

            if (string.IsNullOrWhiteSpace(vm.Telefone) || vm.Telefone.Length < 10)
                r.Erros.Add("O Telefone precisa ter no mínimo 10 dígitos (DDD + número).");

            return r;
        }

        // ——— Métodos de Apoio (Helpers) ———
        private string ApenasNumeros(string entrada)
            => string.IsNullOrWhiteSpace(entrada)
                ? string.Empty
                : new string(entrada.Where(char.IsDigit).ToArray());

        private bool EhEmailValido(string email)
            => !string.IsNullOrWhiteSpace(email) &&
               System.Net.Mail.MailAddress.TryCreate(email, out _);

        private bool CpfEhValido(string cpf)
        {
            // O CPF já deve vir limpo (apenas números) para este método
            if (string.IsNullOrWhiteSpace(cpf) || cpf.Length != 11 || cpf.All(c => c == cpf[0]))
                return false;

            int[] mult1 = { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] mult2 = { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };
            
            string tempCpf = cpf.Substring(0, 9);
            int soma = 0;

            for (int i = 0; i < 9; i++)
                soma += int.Parse(tempCpf[i].ToString()) * mult1[i];
            
            int resto = soma % 11;
            resto = resto < 2 ? 0 : 11 - resto;

            if (int.Parse(cpf[9].ToString()) != resto)
                return false;

            soma = 0;
            tempCpf = cpf.Substring(0, 10);
            for (int i = 0; i < 10; i++)
                soma += int.Parse(tempCpf[i].ToString()) * mult2[i];
            
            resto = soma % 11;
            resto = resto < 2 ? 0 : 11 - resto;

            return int.Parse(cpf[10].ToString()) == resto;
        }
    }
}