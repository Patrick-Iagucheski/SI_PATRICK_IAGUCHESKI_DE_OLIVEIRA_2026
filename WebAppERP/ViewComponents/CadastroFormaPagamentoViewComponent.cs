using Microsoft.AspNetCore.Mvc;
using WebAppERP.ViewModels;

namespace WebAppERP.ViewComponents;

// Modal de cadastro de forma de pagamento, reaproveitado pela tela de cadastro
// e pelas pesquisas de outras telas. View: Pages/Shared/Components/CadastroFormaPagamento/Default.cshtml.
// Traz o proprio script (Default.cshtml.js, na mesma pasta), incluido uma vez
// por pagina. A gravacao fica no Business (chamado pela tela /Financeiro/FormasPagamento).
public class CadastroFormaPagamentoViewComponent : ComponenteBase
{
    public IViewComponentResult Invoke(string modo = "post")
    {
        // O cadastro de condicao de pagamento ja traz este componente (para o +
        // da forma nas parcelas): o modal (ids unicos) e o script entram so uma
        // vez por pagina.
        if (!PrimeiraVezNaPagina())
            return Content(string.Empty);

        var vm = new CadastroFormaPagamentoViewModel { Modo = modo == "ajax" ? "ajax" : "post" };
        vm.IncluirScript = true;

        return View(vm);
    }
}
