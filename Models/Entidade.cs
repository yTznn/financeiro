using System.ComponentModel.DataAnnotations;
using Financeiro.Models; // Garante que ache Endereco e ContaBancaria

namespace Financeiro.Models
{
    public class Entidade
    {
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string Nome { get; set; } = null!;

        [Required, StringLength(20)]
        public string Sigla { get; set; } = null!;

        [Required, StringLength(14)]
        public string Cnpj { get; set; } = null!;

        public int? ContaBancariaId { get; set; }
        public int? EnderecoId { get; set; }

        public bool Ativo { get; set; } = true;
        public bool VinculaUsuario { get; set; } = true;

        [StringLength(int.MaxValue)]
        public string? Observacao { get; set; }

        // === PROPRIEDADES AUXILIARES (Preenchidas via JOIN no Repo) ===
        public Endereco? EnderecoPrincipal { get; set; }
        
        // [ADICIONADO AGORA PARA CORRIGIR O ERRO]
        public ContaBancaria? ContaBancaria { get; set; } 
    }
}