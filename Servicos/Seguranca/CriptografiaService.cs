using System.Security.Cryptography;
using System.Text;
using Financeiro.Servicos.Seguranca;

namespace Financeiro.Servicos.Seguranca
{
    public class CriptografiaService : ICriptografiaService
    {
        // ✅ Chave de 32 caracteres exatos (AES-256)
        private readonly string _chaveAes = "A7B9C1D2E3F4G5H6I7J8K9L0M1N2O3P4"; // 32 caracteres exatos

        public string CriptografarEmail(string email)
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(_chaveAes);

            // 🔒 Gera IV (Vetor de Inicialização) aleatório de 16 bytes
            aes.GenerateIV();
            using var encryptor = aes.CreateEncryptor();

            byte[] inputBytes = Encoding.UTF8.GetBytes(email);
            byte[] encrypted = encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);

            // Junta IV + dados criptografados
            byte[] resultadoComIV = aes.IV.Concat(encrypted).ToArray();

            return Convert.ToBase64String(resultadoComIV);
        }

        public string DescriptografarEmail(string emailCriptografado)
        {
            byte[] input = Convert.FromBase64String(emailCriptografado);

            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(_chaveAes);

            // Extrai os 16 primeiros bytes como IV
            aes.IV = input.Take(16).ToArray();
            using var decryptor = aes.CreateDecryptor();

            byte[] decrypted = decryptor.TransformFinalBlock(input, 16, input.Length - 16);
            return Encoding.UTF8.GetString(decrypted);
        }

        public string GerarHashSenha(string senha)
        {
            return BCrypt.Net.BCrypt.HashPassword(senha);
        }

        public bool VerificarSenha(string senhaInformada, string senhaHashSalva)
        {
            return BCrypt.Net.BCrypt.Verify(senhaInformada, senhaHashSalva);
        }
    }
}