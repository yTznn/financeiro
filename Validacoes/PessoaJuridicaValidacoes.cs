// Local: Financeiro/Validacoes/PessoaJuridicaValidacoes.cs

using System.Linq;
using Financeiro.Models.ViewModels;

namespace Financeiro.Validacoes
{
    public class PessoaJuridicaValidacoes
    {
        public ResultadoValidacao Validar(PessoaJuridicaViewModel vm)
        {
            var r = new ResultadoValidacao();

            // Limpa formatações antes de validar
            vm.NumeroInscricao = ApenasNumeros(vm.NumeroInscricao);
            vm.Telefone        = ApenasNumeros(vm.Telefone);

            // Validações
            if (string.IsNullOrWhiteSpace(vm.RazaoSocial))
                r.Erros.Add("Razão Social é obrigatória.");

            if (string.IsNullOrWhiteSpace(vm.NomeFantasia))
                r.Erros.Add("Nome Fantasia é obrigatório.");

            if (!CnpjEhValido(vm.NumeroInscricao))
                r.Erros.Add("O CNPJ informado é inválido.");

            if (!EhEmailValido(vm.Email))
                r.Erros.Add("O E-mail informado é inválido ou está vazio.");

            if (string.IsNullOrWhiteSpace(vm.Telefone) || vm.Telefone.Length < 10)
                r.Erros.Add("O Telefone precisa ter no mínimo 10 dígitos (DDD + número).");

            return r;
        }

        // ——— Helpers ———
        private string ApenasNumeros(string entrada)
            => string.IsNullOrWhiteSpace(entrada)
                ? string.Empty
                : new string(entrada.Where(char.IsDigit).ToArray());

        private bool EhEmailValido(string email)
            => !string.IsNullOrWhiteSpace(email) &&
               System.Net.Mail.MailAddress.TryCreate(email, out _);

        private bool CnpjEhValido(string cnpj)
        {
            // O CNPJ já deve vir limpo (apenas números) para este método
            if (string.IsNullOrWhiteSpace(cnpj) || cnpj.Length != 14 || cnpj.All(c => c == cnpj[0]))
                return false;

            int[] mult1 = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] mult2 = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

            string tempCnpj = cnpj.Substring(0, 12);
            int soma = 0;
            for (int i = 0; i < 12; i++)
                soma += int.Parse(tempCnpj[i].ToString()) * mult1[i];
            
            int resto = soma % 11;
            resto = resto < 2 ? 0 : 11 - resto;

            if (int.Parse(cnpj[12].ToString()) != resto)
                return false;
            
            soma = 0;
            tempCnpj += resto;
            for (int i = 0; i < 13; i++)
                soma += int.Parse(tempCnpj[i].ToString()) * mult2[i];

            resto = soma % 11;
            resto = resto < 2 ? 0 : 11 - resto;

            return int.Parse(cnpj[13].ToString()) == resto;
        }
    }
}