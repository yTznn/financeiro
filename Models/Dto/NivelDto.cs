namespace Financeiro.Models.Dto
{
    public class NivelDto
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public bool IsNivel1 { get; set; }
        public bool IsNivel2 { get; set; }
        public bool IsNivel3 { get; set; }
        public bool Ativo { get; set; }
    }
}