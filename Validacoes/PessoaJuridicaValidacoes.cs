// Validacoes/PessoaJuridicaValidacoes.cs
using System.Linq;
using Financeiro.Models.ViewModels;

namespace Financeiro.Validacoes
{
    /// <summary>
    /// Resultado genérico de qualquer validação.
    /// </summary>
    public class ResultadoValidacao
    {
        public bool EhValido => !Erros.Any();
        public List<string> Erros { get; } = new();
    }

    /// <summary>
    /// Regras de negócio/validação para cadastro de Pessoa Jurídica.
    /// </summary>
    public class PessoaJuridicaValidacoes
    {
        public ResultadoValidacao Validar(PessoaJuridicaViewModel vm)
        {
            var r = new ResultadoValidacao();

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
            if (vm.Telefone == null || vm.Telefone.Count(char.IsDigit) < 8)
                r.Erros.Add("Telefone precisa ter ao menos 8 dígitos numéricos.");

            return r;
        }

        // ——— Helpers ———
        private bool EhEmailValido(string email)
            => !string.IsNullOrWhiteSpace(email) &&
               System.Net.Mail.MailAddress.TryCreate(email, out _);

        private bool CnpjEhValido(string cnpjRaw)
        {
            if (string.IsNullOrWhiteSpace(cnpjRaw)) return false;

            var cnpj = new string(cnpjRaw.Where(char.IsDigit).ToArray());
            if (cnpj.Length != 14) return false;

            // Algoritmo completo de verificação dos dígitos pode ser adicionado depois.
            return true;
        }
    }
}