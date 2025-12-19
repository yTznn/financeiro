using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Http; 

namespace Financeiro.Models.ViewModels
{
    public class MovimentacaoViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "A data do pagamento é obrigatória.")]
        [DataType(DataType.Date)]
        [Display(Name = "Data do Pagamento")]
        public DateTime DataMovimentacao { get; set; } = DateTime.Today;

        [Display(Name = "Fornecedor / Beneficiário")]
        [Required(ErrorMessage = "Selecione o fornecedor.")]
        public string FornecedorIdCompleto { get; set; }

        [Required(ErrorMessage = "O histórico/descrição é obrigatório.")]
        [Display(Name = "Histórico / Descrição")]
        public string Historico { get; set; }

        [Required(ErrorMessage = "O valor total é obrigatório.")]
        [Display(Name = "Valor Total Pago")]
        public string ValorTotal { get; set; }

        // --- REFERÊNCIA (COMPETÊNCIA) ---
        [Required(ErrorMessage = "O mês de referência é obrigatório.")]
        [Display(Name = "Período de Referência")]
        public string ReferenciaMesAno { get; set; } // Formato "yyyy-MM" para input type="month"

        public DateTime? DataReferenciaInicio { get; set; }
        public DateTime? DataReferenciaFim { get; set; }

        // --- ANEXO ---
        [Display(Name = "Comprovante / Documento (PDF)")]
        public IFormFile? ArquivoAnexo { get; set; }
        
        // Propriedade apenas para exibir se já existe anexo na edição
        public bool TemAnexoSalvo { get; set; }

        // --- DADOS BANCÁRIOS (Visualização Apenas) ---
        public string? ContaOrigemDescricao { get; set; } // Da Entidade
        public string? ContaDestinoDescricao { get; set; } // Do Fornecedor

        public decimal ValorTotalDecimal
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ValorTotal)) return 0;
                if (decimal.TryParse(ValorTotal, NumberStyles.Currency, new CultureInfo("pt-BR"), out decimal v)) return v;
                if (decimal.TryParse(ValorTotal, NumberStyles.Currency, CultureInfo.InvariantCulture, out decimal vInv)) return vInv;
                return 0;
            }
        }

        public List<MovimentacaoRateioViewModel> Rateios { get; set; } = new List<MovimentacaoRateioViewModel>();
    }

    public class MovimentacaoRateioViewModel
    {
        public int InstrumentoId { get; set; }
        
        // [CORREÇÃO] Substituímos NaturezaId por OrcamentoDetalheId (Item do Orçamento)
        public int OrcamentoDetalheId { get; set; } 
        
        public int? ContratoId { get; set; }

        public string Valor { get; set; }

        public decimal ValorDecimal
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Valor)) return 0;
                if (decimal.TryParse(Valor, NumberStyles.Currency, new CultureInfo("pt-BR"), out decimal v)) return v;
                return 0;
            }
        }
        
        // Auxiliares para exibição
        public string? NomeInstrumento { get; set; }
        public string? NomeItemOrcamento { get; set; } // Antigo NomeNatureza
    }
}