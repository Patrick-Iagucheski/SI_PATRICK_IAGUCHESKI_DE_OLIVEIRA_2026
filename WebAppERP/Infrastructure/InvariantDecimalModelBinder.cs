using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace WebAppERP.Infrastructure;


public class InvariantDecimalModelBinder : IModelBinder
{
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
            bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, "Valor numérico inválido.");
        }

        return Task.CompletedTask;
    }

    // Aceita o ponto do <input type="number"> ("1234.56") e o formato pt-BR
    // ("1.234,56" ou "1,5"): o ultimo separador e o decimal e o outro, de milhar.
    // Antes "1,5" era lido como 15 (virgula tratada como milhar).
    private static string Normalizar(string raw)
    {
        var s = raw.Trim().Replace(" ", "");
        int virgula = s.LastIndexOf(','), ponto = s.LastIndexOf('.');
        if (virgula >= 0 && ponto >= 0)
            return virgula > ponto ? s.Replace(".", "").Replace(',', '.') : s.Replace(",", "");
        return virgula >= 0 ? s.Replace(',', '.') : s;
    }

    private static bool TryParse(string raw, Type type, out object? value)
    {
        value = null;
        var s = Normalizar(raw);
        const NumberStyles estilo = NumberStyles.Float;
        var inv = CultureInfo.InvariantCulture;

        if (type == typeof(decimal) && decimal.TryParse(s, estilo, inv, out var m)) { value = m; return true; }
        if (type == typeof(double) && double.TryParse(s, estilo, inv, out var d)) { value = d; return true; }
        if (type == typeof(float) && float.TryParse(s, estilo, inv, out var f)) { value = f; return true; }
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
