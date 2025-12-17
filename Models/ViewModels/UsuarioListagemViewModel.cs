using System;

namespace Financeiro.Models.ViewModels
{
    public class UsuarioListagemViewModel
    {
        public int Id { get; set; }

        public string NameSkip { get; set; }

        public string Email { get; set; } // O Controller vai descriptografar e jogar aqui

        public bool Ativo { get; set; }

        public string? HashImagem { get; set; }

        // --- Propriedades preenchidas via JOIN no Repositório ---
        
        public string? NomePessoaFisica { get; set; }

        public string? NomePerfil { get; set; } // Essencial para a View Premium

        public DateTime? UltimoAcesso { get; set; }
    }
}