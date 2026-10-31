using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;
using WebAppERP.ViewModels;

namespace WebAppERP.ViewComponents;

// Modal de cadastro de produto (com cadastro rapido de marca), reaproveitado
// pela tela de Produtos e pela pesquisa de produto da Compra.
// View: Pages/Shared/Components/CadastroProduto/Default.cshtml.
// Script: wwwroot/js/cadastro-produto.js. A gravacao fica em ProdutosModel
// (OnPostAsync / OnPostSalvarAjaxAsync).
public class CadastroProdutoViewComponent : ViewComponent
{
    private readonly AppDbContext _db;

    public CadastroProdutoViewComponent(AppDbContext db) => _db = db;

    public async Task<IViewComponentResult> InvokeAsync(string modo = "post")
    {
        var vm = new CadastroProdutoViewModel
        {
            Modo = modo == "ajax" ? "ajax" : "post",
            Marcas = await _db.Marcas.Where(m => m.FlAtivo).OrderBy(m => m.NmMarca).ToListAsync(),
            Categorias = await _db.Categorias.Where(c => c.FlAtivo).OrderBy(c => c.NmCategoria).ToListAsync()
        };

        return View(vm);
    }
}
