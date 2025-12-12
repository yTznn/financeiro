using System.ComponentModel.DataAnnotations.Schema;

namespace Financeiro.Models
{
    [Table("ContratoVersaoNatureza")]
    public class ContratoVersaoNatureza
    {
        public int Id { get; set; }
        public int ContratoVersaoId { get; set; }
        public int NaturezaId { get; set; }
        public decimal Valor { get; set; }

        // Propriedade auxiliar para facilitar leitura (não mapeada no insert simples do Dapper, mas útil em consultas)
        [NotMapped]
        public string NomeNatureza { get; set; }
    }
}