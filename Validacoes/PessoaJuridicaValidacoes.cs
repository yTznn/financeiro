using System.Linq;
using Financeiro.Models.ViewModels;
using System.Text.RegularExpressions;

namespace Financeiro.Validacoes
{
    public class ResultadoValidacao
    {
        public bool EhValido => !Erros.Any();
        public List<string> Erros { get; } = new();
    }

    public class PessoaJuridicaValidacoes
    {
        public ResultadoValidacao Validar(PessoaJuridicaViewModel vm)
        {
            var r = new ResultadoValidacao();

            // Limpa formatações antes de validar
            vm.NumeroInscricao = ApenasNumeros(vm.NumeroInscricao);
            vm.Telefone        = ApenasNumeros(vm.Telefone);

            // Razão Social
            if (string.IsNullOrWhiteSpace(vm.RazaoSocial))
                r.Erros.Add("Razão Social é obrigatória.");

            // Nome Fantasia
            if (string.IsNullOrWhiteSpace(vm.NomeFantasia))
                r.Erros.Add("Nome Fantasia é obrigatório.");

            // CNPJ
            if (!CnpjEhValido(vm.NumeroInscricao))
                r.Erros.Add("CNPJ inválido ou em branco.");

            // E-mail
            if (!EhEmailValido(vm.Email))
                r.Erros.Add("E-mail inválido ou vazio.");

            // Telefone (mínimo 8 dígitos numéricos)
            if (string.IsNullOrWhiteSpace(vm.Telefone) || vm.Telefone.Count(char.IsDigit) < 8)
                r.Erros.Add("Telefone precisa ter ao menos 8 dígitos numéricos.");

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
            => !string.IsNullOrWhiteSpace(cnpj) && cnpj.Length == 14;
    }
}