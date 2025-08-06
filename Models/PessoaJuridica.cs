// Models/PessoaJuridica.cs
namespace Financeiro.Models
{
    /// <summary>
    /// Entidade persistida no banco (tabela PessoaJuridica).
    /// </summary>
    public class PessoaJuridica
    {
        public int    Id              { get; set; }
        public string RazaoSocial     { get; set; }
        public string NomeFantasia    { get; set; }
        public string NumeroInscricao { get; set; }  // CNPJ
        public string Email           { get; set; }
        public string Telefone        { get; set; }
        public bool   SituacaoAtiva   { get; set; }
    }
}