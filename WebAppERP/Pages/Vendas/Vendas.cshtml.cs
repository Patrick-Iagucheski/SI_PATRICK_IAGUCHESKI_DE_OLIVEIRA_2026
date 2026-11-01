using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebAppERP.Business.Vendas;

namespace WebAppERP.Pages.Vendas;

// Lista de vendas: so conversa com a tela. As regras ficam em VendaBusiness.
public class VendasModel : PageModel
{
    private readonly VendaBusiness _business;

    public VendasModel(VendaBusiness business) => _business = business;

    public List<Venda> Vendas { get; set; } = [];

    public async Task OnGet()
    {
        Vendas = await _business.Listar();
    }

    public async Task<IActionResult> OnPostCancelar(int id)
    {
        try
        {
            await _business.Cancelar(id);
            TempData["Sucesso"] = "Venda cancelada. O estoque dos produtos foi devolvido.";
        }
        catch (ValidacaoException ex)
        {
            TempData["Erro"] = ex.MensagemCompleta;
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostExcluir(int id)
    {
        try
        {
            await _business.Excluir(id);
            TempData["Sucesso"] = "Venda excluída com sucesso.";
        }
        catch (ValidacaoException ex)
        {
            TempData["Erro"] = ex.MensagemCompleta;
        }
        return RedirectToPage();
    }
}
