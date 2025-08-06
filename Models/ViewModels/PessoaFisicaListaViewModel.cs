using Financeiro.Models;

namespace Financeiro.Models.ViewModels
{
    public class PessoaFisicaListaViewModel
    {
        public PessoaFisica Pessoa { get; set; }

        // Já pensando em endereço futuro, deixamos só a flag de conta agora
        public bool PossuiConta { get; set; }
    }
}