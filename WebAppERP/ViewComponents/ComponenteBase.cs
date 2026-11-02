using Microsoft.AspNetCore.Mvc;

namespace WebAppERP.ViewComponents;

// Base dos componentes reaproveitados entre telas (cadastros em modal, campo
// de cidade). O componente pode aparecer mais de uma vez na mesma pagina, mas
// o script e os modais auxiliares devem entrar so uma vez (ids unicos).
public abstract class ComponenteBase : ViewComponent
{
    // true so na primeira chamada deste componente na requisicao atual.
    protected bool PrimeiraVezNaPagina()
    {
        var chave = $"{GetType().Name}.Incluido";
        if (HttpContext.Items.ContainsKey(chave))
            return false;

        HttpContext.Items[chave] = true;
        return true;
    }
}
