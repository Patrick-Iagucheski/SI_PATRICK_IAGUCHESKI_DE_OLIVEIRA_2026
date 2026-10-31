using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;
using WebAppERP.Models;
using WebAppERP.ViewModels;

namespace WebAppERP.ViewComponents;

// Modal de cadastro de fornecedor, reaproveitado pela tela de Fornecedores e
// pela pesquisa de fornecedor da Compra. A cidade usa o componente unico
// CampoCidade. View: Pages/Shared/Components/CadastroFornecedor/Default.cshtml.
// Script: wwwroot/js/cadastro-fornecedor.js. A gravacao fica em
// FornecedoresModel (OnPostAsync / OnPostSalvarAjaxAsync).
public class CadastroFornecedorViewComponent : ViewComponent
{
    private readonly AppDbContext _db;

    public CadastroFornecedorViewComponent(AppDbContext db) => _db = db;

    public async Task<IViewComponentResult> InvokeAsync(string modo = "post", GerFornecedor? form = null)
    {
        var vm = new CadastroFornecedorViewModel
        {
            FornecedorForm = form ?? new GerFornecedor(),
            Modo = modo == "ajax" ? "ajax" : "post",
            Condicoes = await _db.CondicoesPagamento.Where(c => c.FlAtivo).OrderBy(c => c.DsCondicao).ToListAsync()
        };

        return View(vm);
    }
}
