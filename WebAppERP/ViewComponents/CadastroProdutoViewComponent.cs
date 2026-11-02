using Microsoft.AspNetCore.Mvc;
using WebAppERP.Business.Estoque;
using WebAppERP.ViewModels;

namespace WebAppERP.ViewComponents;

// Modal de cadastro de produto (com cadastro rapido de marca), reaproveitado
// pela tela de Produtos e pela pesquisa de produto da Compra.
// View: Pages/Shared/Components/CadastroProduto/Default.cshtml.
// Traz o proprio script (Default.cshtml.js, na mesma pasta), incluido uma vez
// por pagina. A gravacao fica em ProdutosModel (OnPost / OnPostSalvarAjax).
public class CadastroProdutoViewComponent : ComponenteBase
{
    private readonly MarcaBusiness _marca;
    private readonly CategoriaBusiness _categoria;

    public CadastroProdutoViewComponent(MarcaBusiness marca, CategoriaBusiness categoria)
    {
        _marca = marca;
        _categoria = categoria;
    }

    public async Task<IViewComponentResult> InvokeAsync(string modo = "post")
    {
        var vm = new CadastroProdutoViewModel
        {
            Modo = modo == "ajax" ? "ajax" : "post",
            Marcas = await _marca.Listar(),
            Categorias = await _categoria.Listar()
        };

        // O script entra so na primeira vez que o componente aparece na pagina.
        vm.IncluirScript = PrimeiraVezNaPagina();

        return View(vm);
    }
}
