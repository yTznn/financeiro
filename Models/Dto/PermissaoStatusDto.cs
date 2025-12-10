namespace Financeiro.Models.Dto
{
    public class PermissaoStatusDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Chave { get; set; } = string.Empty;
        public string Modulo { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        
        // Indica se o registro existe na tabela UsuarioPermissoes
        public bool TemPeloUsuario { get; set; } 
        
        // Indica se o registro existe na tabela PerfilPermissoes
        public bool TemPeloPerfil { get; set; }  
    }
}