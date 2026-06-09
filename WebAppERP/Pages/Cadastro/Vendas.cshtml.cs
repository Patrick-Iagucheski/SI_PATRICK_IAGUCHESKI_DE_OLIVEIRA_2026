using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebAppERP.Models;
using WebAppERP.Repositories;

namespace WebAppERP.Pages.Cadastro;

public class VendasModel : PageModel
{
    private readonly IVendaRepository _repo;

    public VendasModel(IVendaRepository repo) => _repo = repo;

    public List<OpeVenda> Vendas { get; set; } = [];

    public async Task OnGetAsync()
    {
        Vendas = await _repo.ListarAsync();
    }

    public async Task<IActionResult> OnPostCancelarAsync(int id)
    {
        await _repo.CancelarAsync(id);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        await _repo.ExcluirAsync(id);
        return RedirectToPage();
    }
}
