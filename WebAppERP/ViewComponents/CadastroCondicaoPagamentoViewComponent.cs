using Microsoft.AspNetCore.Mvc;
using WebAppERP.Business.Financeiro;
using WebAppERP.ViewModels;

namespace WebAppERP.ViewComponents;

// Modal de cadastro de condicao de pagamento (com a grade de parcelas),
// reaproveitado pela tela de Condicoes de Pagamento e pelo + do cadastro de
// fornecedor. Traz o proprio script (Default.cshtml.js, na mesma pasta da view).
// View: Pages/Shared/Components/CadastroCondicaoPagamento/Default.cshtml.
// A gravacao fica em CondicaoPagamentoBusiness (chamado por CondicoesPagamentoModel).
public class CadastroCondicaoPagamentoViewComponent : ComponenteBase
{
    private readonly FormaPagamentoBusiness _forma;

    public CadastroCondicaoPagamentoViewComponent(FormaPagamentoBusiness forma) => _forma = forma;

    public async Task<IViewComponentResult> InvokeAsync(string modo = "post")
    {
        var vm = new CadastroCondicaoPagamentoViewModel
        {
            Modo = modo == "ajax" ? "ajax" : "post",
            Formas = await _forma.Listar(somenteAtivas: true)
        };

        // O script entra so na primeira vez que o componente aparece na pagina.
        vm.IncluirScript = PrimeiraVezNaPagina();

        return View(vm);
    }
}
