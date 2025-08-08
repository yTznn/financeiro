namespace Financeiro.Utils
{
    public static class DocumentoUtils
    {
        public static string ApenasNumeros(string txt) =>
            string.IsNullOrWhiteSpace(txt)
                ? string.Empty
                : new string(txt.Where(char.IsDigit).ToArray());

        public static bool CnpjEhValido(string cnpj)
        {
            cnpj = ApenasNumeros(cnpj);
            if (cnpj.Length != 14 || cnpj.All(c => c == cnpj[0])) return false;

            int[] m1 = { 5,4,3,2,9,8,7,6,5,4,3,2 };
            int[] m2 = { 6,5,4,3,2,9,8,7,6,5,4,3,2 };

            int soma = 0;
            for (int i = 0; i < 12; i++) soma += (cnpj[i]-'0') * m1[i];
            int resto = soma % 11;
            int dig1 = resto < 2 ? 0 : 11 - resto;

            soma = 0;
            for (int i = 0; i < 13; i++) soma += ( (i==12?dig1:cnpj[i]-'0') ) * m2[i];
            resto = soma % 11;
            int dig2 = resto < 2 ? 0 : 11 - resto;

            return cnpj.EndsWith($"{dig1}{dig2}");
        }
    }
}