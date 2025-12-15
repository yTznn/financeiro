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

        [Display(Name = "Fornecedor (Opcional)")]
        public string? FornecedorIdCompleto { get; set; }

        [Required(ErrorMessage = "O histórico/descrição é obrigatório.")]
        [Display(Name = "Histórico / Descrição")]
        public string Historico { get; set; }

        [Required(ErrorMessage = "O valor total é obrigatório.")]
        [Display(Name = "Valor Total Pago")]
        public string ValorTotal { get; set; }

        // --- [NOVOS CAMPOS DE REFERÊNCIA] ---
        
        // 1. Campo Visual (para o input type="month" da View)
        // O navegador envia string no formato "yyyy-MM" (ex: "2025-02")
        [Display(Name = "Mês de Referência")]
        public string? ReferenciaMesAno { get; set; }

        // 2. Campos de Banco (Calculados no Controller a partir do campo acima)
        public DateTime? DataReferenciaInicio { get; set; }
        public DateTime? DataReferenciaFim { get; set; }

        // ------------------------------------

        [Display(Name = "Comprovante / Documento (PDF)")]
        public IFormFile? ArquivoAnexo { get; set; }

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
        public int NaturezaId { get; set; }
        public int? ContratoId { get; set; }

        public string Valor { get; set; }

        public decimal ValorDecimal
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Valor)) return 0;
                if (decimal.TryParse(Valor, NumberStyles.Currency, new CultureInfo("pt-BR"), out decimal v)) return v;
                if (decimal.TryParse(Valor, NumberStyles.Currency, CultureInfo.InvariantCulture, out decimal vInv)) return vInv;
                return 0;
            }
        }
        
        public string? NomeInstrumento { get; set; }
        public string? NomeNatureza { get; set; }
    }
}