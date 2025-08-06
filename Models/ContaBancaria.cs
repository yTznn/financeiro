namespace Financeiro.Models
{
    public class ContaBancaria
    {
        public int    Id        { get; set; }
        public string Banco     { get; set; }   // ex.: Banco do Brasil
        public string Agencia   { get; set; }   // “1234-5”
        public string Conta     { get; set; }   // “987654-0”
        public string ChavePix  { get; set; }   // CPF, CNPJ, telefone, e-mail ou aleatória
    }
}