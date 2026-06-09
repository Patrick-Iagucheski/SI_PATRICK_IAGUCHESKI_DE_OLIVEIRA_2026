using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;
using WebAppERP.Models;

namespace WebAppERP.Pages.Cadastro;

public class EstadosModel : PageModel
{
    private readonly AppDbContext _db;

    public EstadosModel(AppDbContext db) => _db = db;

    public List<GerEstado> Estados { get; set; } = [];
    public List<GerPais> Paises { get; set; } = [];

    [BindProperty]
    public GerEstado EstadoForm { get; set; } = new();

    public async Task OnGetAsync()
    {
        Estados = await _db.Estados
            .Include(e => e.Pais)
            .OrderBy(e => e.NmEstado)
            .ToListAsync();

        Paises = await _db.Paises.OrderBy(p => p.NmPais).ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (EstadoForm.IdEstado == 0)
        {
            EstadoForm.DtCriacao = DateTime.Now;
            _db.Estados.Add(EstadoForm);
        }
        else
        {
            var existing = await _db.Estados.FindAsync(EstadoForm.IdEstado);
            if (existing != null)
            {
                existing.SgUF = EstadoForm.SgUF;
                existing.NmEstado = EstadoForm.NmEstado;
                existing.IdPais = EstadoForm.IdPais;
                existing.DtEdicao = DateTime.Now;
            }
        }

        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var estado = await _db.Estados.FindAsync(id);
        if (estado != null)
        {
            _db.Estados.Remove(estado);
            await _db.SaveChangesAsync();
        }
        return RedirectToPage();
    }
}
