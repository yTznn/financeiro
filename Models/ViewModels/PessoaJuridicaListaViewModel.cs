using Financeiro.Models;

namespace Financeiro.Models.ViewModels
{
    public class PessoaJuridicaListaViewModel
    {
        public PessoaJuridica Pessoa { get; set; }
        public bool PossuiEndereco { get; set; }
        public bool PossuiConta    { get; set; }
    }
}