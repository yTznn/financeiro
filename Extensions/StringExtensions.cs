using System.Globalization;
using System.Linq;
using System.Text;

namespace Financeiro.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Remove acentos e caracteres especiais (não ASCII) de uma string.
        /// Útil para URLs, nomes de arquivos e cabeçalhos HTTP.
        /// </summary>
        public static string RemoverAcentosEspeciais(this string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return texto;

            // 1. Normaliza a string para remover acentos (ç -> c, ã -> a)
            var normalizedString = texto.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            // 2. Remove caracteres inválidos restantes (como espaços múltiplos, /,\, etc.)
            string resultado = stringBuilder.ToString().Normalize(NormalizationForm.FormC);
            
            // Substitui caracteres que são comuns em nomes de arquivo/url por '-' ou remove
            resultado = resultado.Replace(" ", "_").Replace(":", "-").Replace(",", "").Replace("'", "");
            
            // Remove o cê-cedilha (`ç` após normalização) se ainda restar algum
            resultado = resultado.Replace('ç', 'c').Replace('Ç', 'C');
            
            // Deixa apenas caracteres alfanuméricos e '_' e '-'
            resultado = new string(resultado.Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-').ToArray());

            return resultado;
        }
    }
}