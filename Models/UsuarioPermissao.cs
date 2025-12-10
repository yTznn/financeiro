namespace Financeiro.Models
{
    public class UsuarioPermissao
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public int PermissaoId { get; set; }
        
        // Define se está concedendo (true) ou bloqueando (false)
        // No seu caso inicial, usaremos mais para CONCEDER permissões extras.
        public bool Concedido { get; set; } = true;

        // Propriedade de navegação (opcional, ajuda em joins se fosse EF, mas útil para Dapper multimap)
        public Permissao? Permissao { get; set; }
    }
}