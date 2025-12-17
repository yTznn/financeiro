using System;
using System.Collections.Generic;

namespace Financeiro.Models.ViewModels
{
    // ViewModel para os detalhes de cada linha de lançamento no relatório
    public class LancamentoDetalheViewModel
    {
        public int LancamentoSequencial { get; set; }
        public DateTime DataLancamento { get; set; }
        public decimal ValorLancamento { get; set; } // Valor da Saída
        public decimal SaldoCumulativo { get; set; } // Saldo após este lançamento
    }

    // ViewModel principal que consolida o cabeçalho e os lançamentos
    public class RelatorioInstrumentoViewModel
    {
        // CABEÇALHO DO RELATÓRIO
        public string InstrumentoNumero { get; set; }
        public string ReferenciaPeriodo { get; set; } // Ex: "jan/25"
        public DateTime DataInicioPeriodo { get; set; }
        public DateTime DataFimPeriodo { get; set; }
        public decimal SaldoInicialMes { get; set; }
        public bool PossuiRecebimento { get; set; } // CRÍTICO: Para colorir o relatório

        // RODAPÉ E AGREGADOS
        public decimal SaldoTotalFinal { get; set; }
        public decimal ValorTotalSaidas { get; set; }

        // DETALHES DA TABELA
        public List<LancamentoDetalheViewModel> Lancamentos { get; set; } = new List<LancamentoDetalheViewModel>();
    }
}