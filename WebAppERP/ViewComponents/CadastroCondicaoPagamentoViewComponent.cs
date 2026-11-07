using Microsoft.AspNetCore.Mvc;
using WebAppERP.Business.Financeiro;
using WebAppERP.ViewModels;

namespace WebAppERP.ViewComponents;

// Modal de cadastro de condicao de pagamento (com a grade de parcelas),
// reaproveitado pela tela de Condicoes de Pagamento e pela pesquisa de condicao
// (cadastro de fornecedor e Compra). Traz o proprio script (Default.cshtml.js, na mesma pasta da view).
// View: Pages/Shared/Components/CadastroCondicaoPagamento/Default.cshtml.
// A gravacao fica em CondicaoPagamentoBusiness (chamado por CondicoesPagamentoModel).
public class CadastroCondicaoPagamentoViewComponent : ComponenteBase
{
    private readonly FormaPagamentoBusiness _forma;

    public CadastroCondicaoPagamentoViewComponent(FormaPagamentoBusiness forma) => _forma = forma;

    public async Task<IViewComponentResult> InvokeAsync(string modo = "post")
    {
        // Cliente e fornecedor trazem este cadastro junto: o modal (ids unicos)
        // entra so uma vez por pagina.
        if (!PrimeiraVezNaPagina())
            return Content(string.Empty);

        var vm = new CadastroCondicaoPagamentoViewModel
        {
            Modo = modo == "ajax" ? "ajax" : "post",
            Formas = await _forma.Listar(somenteAtivas: true)
        };

        vm.IncluirScript = true;

        return View(vm);
    }
}
