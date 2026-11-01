using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebAppERP.Business.Vendas;

namespace WebAppERP.Pages.Vendas;

public class VendasModel : PageModel
{
    private readonly VendaBusiness _business;

    public VendasModel(VendaBusiness repo) => _business = repo;

    public List<Venda> Vendas { get; set; } = [];

    public async Task OnGet()
    {
        Vendas = await _business.Listar();
    }

    public async Task<IActionResult> OnPostCancelar(int id)
    {
        await _business.Cancelar(id);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostExcluir(int id)
    {
        // O repositorio ja remove os itens antes da venda e estorna o estoque.
        // O catch cobre qualquer FK futura que passe a apontar para tbVenVendas.
        try
        {
            await _business.Excluir(id);
            TempData["Sucesso"] = "Venda excluída com sucesso.";
        }
        catch (DbUpdateException)
        {
            TempData["Erro"] = "Não é possível excluir: existem registros vinculados a esta venda.";
        }
        return RedirectToPage();
    }
}
