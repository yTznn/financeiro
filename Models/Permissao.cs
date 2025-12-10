namespace Financeiro.Models
{
    public class Permissao
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Chave { get; set; } = string.Empty; // Ex: INSTRUMENTO_ADD
        public string Modulo { get; set; } = string.Empty; // Ex: Instrumentos
        public string? Descricao { get; set; }
        public bool Ativo { get; set; }
    }
}