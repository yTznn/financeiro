using System;

namespace Financeiro.Models
{
    /// <summary>
    /// Representa um registro histórico (versão) de um Contrato.
    /// </summary>
    public class ContratoVersao
    {
        public int Id { get; set; }
        public int ContratoId { get; set; } // FK para a tabela Contrato
        public int Versao { get; set; }     // 1 = Original, 2 = 1º Aditivo, etc.

        // Campos que podem ser alterados pelo aditivo
        public string ObjetoContrato { get; set; } = string.Empty;
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public decimal ValorContrato { get; set; }

        // Metadados do aditivo
        public TipoAditivo? TipoAditivo { get; set; } // Reutilizando o enum já existente
        public string? Observacao { get; set; }
        public DateTime DataRegistro { get; set; } = DateTime.Now;
        public DateTime? DataInicioAditivo { get; set; }

    }
}