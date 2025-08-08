namespace Financeiro.Models
{
    /// <summary>
    /// Vínculo N-para-N entre Usuário e Entidade.
    /// </summary>
    public class UsuarioEntidade
    {
        public int  Id         { get; set; }
        public int  UsuarioId  { get; set; }
        public int  EntidadeId { get; set; }
        public bool Ativo      { get; set; }   // Indica entidade ativa do usuário
    }
}