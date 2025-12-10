using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Financeiro.Extensions; // Usa a extensão que acabamos de criar

namespace Financeiro.Atributos
{
    public class AutorizarPermissaoAttribute : ActionFilterAttribute
    {
        private readonly string _permissaoNecessaria;

        public AutorizarPermissaoAttribute(string permissao)
        {
            _permissaoNecessaria = permissao;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // 1. Se não estiver logado, manda pro login
            if (context.HttpContext.User == null || !context.HttpContext.User.Identity.IsAuthenticated)
            {
                context.Result = new RedirectResult("/Conta/Login");
                return;
            }

            // 2. Verifica a permissão usando nossa extensão
            if (!context.HttpContext.User.TemPermissao(_permissaoNecessaria))
            {
                // Se não tiver permissão, retorna Erro 403 (Proibido)
                context.Result = new ContentResult()
                {
                    StatusCode = 403,
                    Content = $"Acesso Negado: Você precisa da permissão '{_permissaoNecessaria}' para realizar esta ação."
                };
                
                // Opcional: Se preferir redirecionar para uma página bonita:
                // context.Result = new RedirectToActionResult("AcessoNegado", "Home", null);
            }
            
            base.OnActionExecuting(context);
        }
    }
}