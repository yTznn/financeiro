using System;
using System.Security.Claims;

namespace Financeiro.Extensions
{
    public static class UserExtensions
    {
        /// <summary>
        /// Verifica se o usuário logado possui uma permissão específica (via Claims).
        /// </summary>
        public static bool TemPermissao(this ClaimsPrincipal user, string permissao)
        {
            if (user == null || user.Identity == null || !user.Identity.IsAuthenticated) 
                return false;

            // Verifica se existe alguma claim do tipo "Permissao" com o valor desejado
            return user.HasClaim(c => c.Type == "Permissao" && c.Value == permissao);
        }

        /// <summary>
        /// Obtém o ID da Entidade (Unidade) logada no momento.
        /// Retorna 0 se não encontrar.
        /// </summary>
        public static int ObterEntidadeId(this ClaimsPrincipal user)
        {
            if (user == null || user.Identity == null || !user.Identity.IsAuthenticated) 
                return 0;

            // Busca a claim "EntidadeId" que gravamos no ContaController/Login
            var claim = user.FindFirst("EntidadeId");
            
            if (claim != null && int.TryParse(claim.Value, out int id))
            {
                return id;
            }
            
            return 0;
        }

        /// <summary>
        /// Obtém a Sigla da Entidade (ex: "IPGSE") para exibição.
        /// </summary>
        public static string ObterSiglaEntidade(this ClaimsPrincipal user)
        {
            if (user == null || user.Identity == null || !user.Identity.IsAuthenticated)
                return string.Empty;

            return user.FindFirst("SiglaEntidade")?.Value ?? string.Empty;
        }
    }
}