// Financeiro/Models/ViewModels/OrcamentoListViewModel.cs
using System;
using System.Collections.Generic; // Adicionado para usar List<>
using System.ComponentModel.DataAnnotations;

namespace Financeiro.Models.ViewModels
{
    // 1. Adicione esta classe pequena para representar as linhas do BI
    public class OrcamentoDetalheBI
    {
        public string NomeItem { get; set; }
        public decimal ValorConsumido { get; set; }
        public decimal PercentualDoTotal { get; set; } 
    }

    // 2. Sua classe principal atualizada
    public class OrcamentoListViewModel
    {
        public int Id { get; set; }
        
        [Display(Name = "Nome do Orçamento")]
        public string Nome { get; set; } = string.Empty;
        
        public string? Observacao { get; set; }
        
        [Display(Name = "Vigência Início")]
        public DateTime VigenciaInicio { get; set; }
        
        [Display(Name = "Vigência Fim")]
        public DateTime VigenciaFim { get; set; }
        
        [Display(Name = "Valor Orçado")]
        public decimal ValorPrevistoTotal { get; set; }
        
        public bool Ativo { get; set; }

        [Display(Name = "Valor Comprometido")]
        public decimal ValorComprometido { get; set; }

        // Corrigi para ser apenas get calculado, caso não venha do banco
        [Display(Name = "Saldo Disponível")]
        public decimal SaldoDisponivel => ValorPrevistoTotal - ValorComprometido;

        [Display(Name = "Instrumento")]
        public string InstrumentoNome { get; set; } = string.Empty;

        // --- NOVO CAMPO: Lista para o BI da Home ---
        public List<OrcamentoDetalheBI> DetalhamentoConsumo { get; set; } = new List<OrcamentoDetalheBI>();
    }
}