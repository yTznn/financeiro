namespace Financeiro.Models
{
    /// <summary>
    /// Liga uma ContaBancaria a uma pessoa (PJ ou PF).
    /// Exatamente um dos IDs deve ser preenchido.
    /// </summary>
    public class PessoaConta
    {
        public int Id                { get; set; }
        public int? PessoaJuridicaId { get; set; }
        public int? PessoaFisicaId   { get; set; }
        public int ContaBancariaId   { get; set; }
    }
}