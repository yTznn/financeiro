using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;

namespace Financeiro.Models.ViewModels
{
    // 1. A classe que representa uma linha na grid de itens do contrato
    public class ContratoItemViewModel
    {
        public int Id { get; set; } 
        public string NomeItem { get; set; } = string.Empty;
        public decimal Valor { get; set; } // Representa o Valor TOTAL deste item no contrato
    }
}