namespace Financeiro.Models
{
    public class Endereco
    {
        public int    Id          { get; set; }
        public string Logradouro  { get; set; }   // Rua, avenida…
        public string Numero      { get; set; }   // “123” ou “SN”
        public string Complemento { get; set; }   // Quadra, lote, apto…
        public string Cep         { get; set; }   // 75920000
        public string Bairro      { get; set; }
        public string Municipio   { get; set; }
        public string Uf          { get; set; }   // GO, SP…
    }
}