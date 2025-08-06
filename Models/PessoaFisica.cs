namespace Financeiro.Models
{
    public class PessoaFisica
    {
        public int      Id             { get; set; }
        public string   Nome           { get; set; }
        public string   Sobrenome      { get; set; }
        public string   Cpf            { get; set; }
        public DateTime DataNascimento { get; set; }
        public string   Email          { get; set; }
        public string   Telefone       { get; set; }
        public bool     SituacaoAtiva  { get; set; }
    }
}