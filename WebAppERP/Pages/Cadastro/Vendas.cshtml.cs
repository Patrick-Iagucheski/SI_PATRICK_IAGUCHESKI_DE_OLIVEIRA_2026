using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
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
        // O repositorio ja remove os itens antes da venda e estorna o estoque.
        // O catch cobre qualquer FK futura que passe a apontar para tbOpeVendas.
        try
        {
            await _repo.ExcluirAsync(id);
            TempData["Sucesso"] = "Venda excluída com sucesso.";
        }
        catch (DbUpdateException)
        {
            TempData["Erro"] = "Não é possível excluir: existem registros vinculados a esta venda.";
        }
        return RedirectToPage();
    }
}
