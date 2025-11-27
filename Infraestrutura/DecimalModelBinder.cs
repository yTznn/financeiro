using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace Financeiro.Infraestrutura
{
    public class DecimalModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);

            if (valueProviderResult == ValueProviderResult.None)
            {
                return Task.CompletedTask;
            }

            bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);

            var valueAsString = valueProviderResult.FirstValue;

            if (string.IsNullOrEmpty(valueAsString))
            {
                return Task.CompletedTask;
            }

            // Remove espaços e tenta converter usando cultura Brasileira
            // Aceita: 25000,00 | 25.000,00
            decimal result;
            var culture = new CultureInfo("pt-BR");
            
            // Tenta converter. Se falhar, tenta remover os pontos de milhar manualmente e converter de novo
            if (decimal.TryParse(valueAsString, NumberStyles.Any, culture, out result))
            {
                bindingContext.Result = ModelBindingResult.Success(result);
            }
            else
            {
                // Fallback: Se veio "25000.00" (formato americano por algum motivo do JS)
                var cultureUS = new CultureInfo("en-US");
                if (decimal.TryParse(valueAsString, NumberStyles.Any, cultureUS, out result))
                {
                    bindingContext.Result = ModelBindingResult.Success(result);
                }
                else
                {
                    // Se não conseguiu ler de jeito nenhum, adiciona erro
                    bindingContext.ModelState.TryAddModelError(
                        bindingContext.ModelName,
                        "Formato de valor inválido.");
                }
            }

            return Task.CompletedTask;
        }
    }

    public class DecimalModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            if (context.Metadata.ModelType == typeof(decimal) || context.Metadata.ModelType == typeof(decimal?))
            {
                return new DecimalModelBinder();
            }

            return null;
        }
    }
}