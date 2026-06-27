using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace WebAppERP.Infrastructure;


public class InvariantDecimalModelBinder : IModelBinder
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var valueResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueResult == ValueProviderResult.None)
            return Task.CompletedTask;

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueResult);

        var raw = valueResult.FirstValue;
        var underlying = Nullable.GetUnderlyingType(bindingContext.ModelType) ?? bindingContext.ModelType;

        // Vazio: deixa o framework tratar (null para nullable / required para os demais).
        if (string.IsNullOrWhiteSpace(raw))
            return Task.CompletedTask;

        if (TryParse(raw, underlying, out var parsed))
        {
            bindingContext.Result = ModelBindingResult.Success(parsed);
        }
        else
        {
            bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, "Valor numerico invalido.");
        }

        return Task.CompletedTask;
    }

    private static bool TryParse(string raw, Type type, out object? value)
    {
        value = null;

        if (type == typeof(decimal))
        {
            if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ||
                decimal.TryParse(raw, NumberStyles.Number, PtBr, out d))
            { value = d; return true; }
        }
        else if (type == typeof(double))
        {
            if (double.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ||
                double.TryParse(raw, NumberStyles.Number, PtBr, out d))
            { value = d; return true; }
        }
        else if (type == typeof(float))
        {
            if (float.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ||
                float.TryParse(raw, NumberStyles.Number, PtBr, out d))
            { value = d; return true; }
        }

        return false;
    }
}

// Aplica o binder acima a todo campo decimal/double/float (e seus Nullable<>).
public class InvariantDecimalModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var type = Nullable.GetUnderlyingType(context.Metadata.ModelType) ?? context.Metadata.ModelType;
        if (type == typeof(decimal) || type == typeof(double) || type == typeof(float))
            return new InvariantDecimalModelBinder();

        return null;
    }
}
