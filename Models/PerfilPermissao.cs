namespace Financeiro.Models
{
    public class PerfilPermissao
    {
        public int Id { get; set; }
        public int PerfilId { get; set; }
        public int PermissaoId { get; set; }
        public Permissao? Permissao { get; set; }
    }
}