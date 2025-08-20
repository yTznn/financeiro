using Financeiro.Models;

namespace Financeiro.Models.ViewModels
{
    /// <summary>
    /// Representa os dados a serem exibidos em cada linha da lista de contratos.
    /// </summary>
    public class ContratoListaViewModel
    {
        // Contém todos os dados do contrato (Número, Valor, Datas, etc.)
        public Contrato Contrato { get; set; } = new Contrato();

        // Contém apenas o nome do fornecedor para exibição
        public string FornecedorNome { get; set; } = string.Empty;

        public int QuantidadeAditivos { get; set; }
    }
}