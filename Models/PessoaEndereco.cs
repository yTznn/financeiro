namespace Financeiro.Models
{
    /// <summary>
    /// Relaciona uma Pessoa Jurídica a um Endereço.
    /// (FKs simples; pode evoluir para PK composta se quiser evitar duplicidade)
    /// </summary>
    public class PessoaEndereco
    {
        public int Id              { get; set; }
        public int PessoaJuridicaId{ get; set; }
        public int EnderecoId      { get; set; }
    }
}