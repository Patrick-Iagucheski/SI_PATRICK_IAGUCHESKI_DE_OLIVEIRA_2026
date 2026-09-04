using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;
using WebAppERP.Models;

namespace WebAppERP.Pages.Cadastro;

public class PaisesModel : PageModel
{
    private readonly AppDbContext _db;

    public PaisesModel(AppDbContext db) => _db = db;

    public List<GerPais> Paises { get; set; } = [];

    [BindProperty]
    public GerPais PaisForm { get; set; } = new();

    public async Task OnGetAsync()
    {
        Paises = await _db.Paises.OrderBy(p => p.NmPais).ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (PaisForm.IdPais == 0)
        {
            _db.Paises.Add(PaisForm);
        }
        else
        {
            var existing = await _db.Paises.FindAsync(PaisForm.IdPais);
            if (existing != null)
            {
                existing.NmPais = PaisForm.NmPais;
                existing.SgPais = PaisForm.SgPais;
                existing.NrDDI = PaisForm.NrDDI;
                existing.DsMoeda = PaisForm.DsMoeda;
            }
        }

        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var pais = await _db.Paises.FindAsync(id);
        if (pais == null)
            return RedirectToPage();

        // Nao deixa excluir pais que ainda tem estados (FK_tbGerEstados_Pais).
        if (await _db.Estados.AnyAsync(e => e.IdPais == id))
        {
            TempData["Erro"] = "Não é possível excluir: existe estado vinculado a este país.";
            return RedirectToPage();
        }

        try
        {
            _db.Paises.Remove(pais);
            await _db.SaveChangesAsync();
            TempData["Sucesso"] = "País excluído com sucesso.";
        }
        catch (DbUpdateException)
        {
            TempData["Erro"] = "Não é possível excluir: existem registros vinculados a este país.";
        }
        return RedirectToPage();
    }
}
