using System;

namespace Financeiro.Models
{
    public class Arquivo
    {
        public int Id { get; set; }
        public string NomeOriginal { get; set; }
        public string Extensao { get; set; }
        public string Hash { get; set; }
        public byte[] Conteudo { get; set; }
        public long Tamanho { get; set; }
        public string ContentType { get; set; }
        public DateTime DataEnvio { get; set; } = DateTime.Now;

        public string? Origem { get; set; }         // Ex: "PerfilUsuario", "Contrato", etc.
        public int? ChaveReferencia { get; set; }   // Id do usuário, contrato, etc.
    }
}