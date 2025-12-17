using System;

namespace Financeiro.Models.ViewModels
{
    public class LogListagemViewModel
    {
        public int Id { get; set; }
        
        // Dados da Ação
        public string Acao { get; set; }        // Ex: Inserir, Atualizar
        public string Tabela { get; set; }      // Ex: Contrato, Usuario
        public DateTime DataHora { get; set; }
        
        // Detalhes (Pode ser o JSON de ValoresNovos ou um resumo)
        public string? ValoresNovos { get; set; } 

        // Dados Cruzados (JOINs)
        public string UsuarioNome { get; set; } // NameSkip (Login)
        public string? NomePessoa { get; set; } // <<-- ESTA LINHA CORRIGE O ERRO CS1061
        public string? EntidadeSigla { get; set; } // Para saber de qual unidade foi a ação
    }
}