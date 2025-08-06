// Local: Financeiro/Validacoes/ResultadoValidacao.cs

using System.Collections.Generic;
using System.Linq;

namespace Financeiro.Validacoes
{
    public class ResultadoValidacao
    {
        public bool EhValido => !Erros.Any();
        public List<string> Erros { get; } = new();
    }
}