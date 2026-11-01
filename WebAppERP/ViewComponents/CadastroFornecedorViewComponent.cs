using Microsoft.AspNetCore.Mvc;
using WebAppERP.Business.Financeiro;
using WebAppERP.ViewModels;

namespace WebAppERP.ViewComponents;

// Modal de cadastro de fornecedor, reaproveitado pela tela de Fornecedores e
// pela pesquisa de fornecedor da Compra. A cidade usa o componente unico
// CampoCidade. View: Pages/Shared/Components/CadastroFornecedor/Default.cshtml.
// Traz o proprio script (Default.cshtml.js, na mesma pasta), incluido uma vez
// por pagina. A gravacao fica em FornecedorBusiness (chamado por FornecedoresModel).
public class CadastroFornecedorViewComponent : ViewComponent
{
    private const string ChaveScriptIncluido = "CadastroFornecedor.ScriptIncluido";
    private readonly CondicaoPagamentoBusiness _condicao;

    public CadastroFornecedorViewComponent(CondicaoPagamentoBusiness condicao) => _condicao = condicao;

    public async Task<IViewComponentResult> InvokeAsync(string modo = "post", Fornecedor? form = null)
    {
        var vm = new CadastroFornecedorViewModel
        {
            FornecedorForm = form ?? new Fornecedor(),
            Modo = modo == "ajax" ? "ajax" : "post",
            Condicoes = await _condicao.Listar(somenteAtivas: true)
        };

        if (!HttpContext.Items.ContainsKey(ChaveScriptIncluido))
        {
            HttpContext.Items[ChaveScriptIncluido] = true;
            vm.IncluirScript = true;
        }

        return View(vm);
    }
}
