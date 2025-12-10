using System.Collections.Generic;

namespace Financeiro.Models.ViewModels
{
    // Representa a tela inteira de gerenciamento de permissões de UM usuário
    public class UsuarioPermissoesEdicaoViewModel
    {
        public int UsuarioId { get; set; }
        public string NomeUsuario { get; set; } = string.Empty;
        public string EmailUsuario { get; set; } = string.Empty;

        // Lista de módulos (Instrumentos, Financeiro, etc) para agrupar na tela
        public List<ModuloPermissoesViewModel> Modulos { get; set; } = new();
    }

    // Representa um Grupo (ex: "Instrumentos")
    public class ModuloPermissoesViewModel
    {
        public string NomeModulo { get; set; } = string.Empty;
        public List<PermissaoCheckViewModel> Permissoes { get; set; } = new();
    }

    // Representa o Checkbox individual (ex: [x] Cadastrar Instrumento)
    public class PermissaoCheckViewModel
    {
        public int PermissaoId { get; set; }
        public string Nome { get; set; } = string.Empty; // "Cadastrar Instrumento"
        public string Chave { get; set; } = string.Empty; // "INSTRUMENTO_ADD"
        public string Descricao { get; set; } = string.Empty;
        
        public bool Concedido { get; set; } // Se está marcado ou não
        public bool HerdadoDoPerfil { get; set; } // Para mostrar se ele já tem isso pelo perfil (apenas visual)
    }
}