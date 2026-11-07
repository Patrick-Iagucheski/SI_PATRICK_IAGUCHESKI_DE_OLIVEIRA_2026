using Microsoft.AspNetCore.Mvc;
using WebAppERP.Business.Financeiro;
using WebAppERP.ViewModels;

namespace WebAppERP.ViewComponents;

// Modal de cadastro de cliente, reaproveitado pela tela de cadastro
// e pelas pesquisas de outras telas. View: Pages/Shared/Components/CadastroCliente/Default.cshtml.
// Traz o proprio script (Default.cshtml.js, na mesma pasta), incluido uma vez
// por pagina. A gravacao fica no Business (chamado pela tela /Gerais/Clientes).
public class CadastroClienteViewComponent : ComponenteBase
{
    private readonly CondicaoPagamentoBusiness _condicao;

    public CadastroClienteViewComponent(CondicaoPagamentoBusiness condicao) => _condicao = condicao;

    public async Task<IViewComponentResult> InvokeAsync(string modo = "post")
    {
        var vm = new CadastroClienteViewModel
        {
            Modo = modo == "ajax" ? "ajax" : "post",
            Condicoes = await _condicao.Listar(somenteAtivas: true)
        };

        // O script entra so na primeira vez que o componente aparece na pagina.
        vm.IncluirScript = PrimeiraVezNaPagina();

        return View(vm);
    }
}
