using System;

namespace Financeiro.Models
{
    public class Usuario
    {
        public int Id { get; set; }

        // Login pode ser feito via nameskip ou e-mail
        public string NameSkip { get; set; } // Ex: elielton.silva
        public string EmailCriptografado { get; set; }

        // Segurança
        public string SenhaHash { get; set; }

        // Foto de perfil (opcional)
        public string? NomeArquivoImagem { get; set; } // Ex: "foto-elielton"
        public string? HashImagem { get; set; }         // Hash único da imagem

        // Relacionamento com Pessoa Física
        public int? PessoaFisicaId { get; set; } // Pode ser null se ainda não vinculado

        // Controle e auditoria
        public bool Ativo { get; set; } = true;
        public DateTime DataCriacao { get; set; } = DateTime.Now;
        public DateTime? UltimoAcesso { get; set; }
    }
}