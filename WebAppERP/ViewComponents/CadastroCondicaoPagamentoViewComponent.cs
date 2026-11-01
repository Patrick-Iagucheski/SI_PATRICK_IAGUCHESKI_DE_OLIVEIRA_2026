using Microsoft.AspNetCore.Mvc;
using WebAppERP.Business.Financeiro;
using WebAppERP.ViewModels;

namespace WebAppERP.ViewComponents;

// Modal de cadastro de condicao de pagamento (com a grade de parcelas),
// reaproveitado pela tela de Condicoes de Pagamento e pelo + do cadastro de
// fornecedor. Traz o proprio script (Default.cshtml.js, na mesma pasta da view).
// View: Pages/Shared/Components/CadastroCondicaoPagamento/Default.cshtml.
// A gravacao fica em CondicaoPagamentoBusiness (chamado por CondicoesPagamentoModel).
public class CadastroCondicaoPagamentoViewComponent : ViewComponent
{
    private const string ChaveScriptIncluido = "CadastroCondicaoPagamento.ScriptIncluido";
    private readonly FormaPagamentoBusiness _forma;

    public CadastroCondicaoPagamentoViewComponent(FormaPagamentoBusiness forma) => _forma = forma;

    public async Task<IViewComponentResult> InvokeAsync(string modo = "post", CondicaoPagamento? form = null)
    {
        var vm = new CadastroCondicaoPagamentoViewModel
        {
            CondicaoForm = form ?? new CondicaoPagamento(),
            Modo = modo == "ajax" ? "ajax" : "post",
            Formas = await _forma.Listar(somenteAtivas: true)
        };

        if (!HttpContext.Items.ContainsKey(ChaveScriptIncluido))
        {
            HttpContext.Items[ChaveScriptIncluido] = true;
            vm.IncluirScript = true;
        }

        return View(vm);
    }
}
