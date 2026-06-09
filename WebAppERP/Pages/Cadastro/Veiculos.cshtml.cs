using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;
using WebAppERP.Models;

namespace WebAppERP.Pages.Cadastro;

public class VeiculosModel : PageModel
{
    private readonly AppDbContext _db;

    public VeiculosModel(AppDbContext db) => _db = db;

    public List<GerVeiculo> Veiculos { get; set; } = [];
    public List<GerEstado> Estados { get; set; } = [];

    [BindProperty]
    public GerVeiculo VeiculoForm { get; set; } = new();

    public async Task OnGetAsync()
    {
        Veiculos = await _db.Veiculos
            .Include(v => v.Estado)
            .OrderBy(v => v.DsPlaca)
            .ToListAsync();

        Estados = await _db.Estados.OrderBy(e => e.NmEstado).ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (VeiculoForm.IdVeiculo == 0)
        {
            _db.Veiculos.Add(VeiculoForm);
        }
        else
        {
            var existing = await _db.Veiculos.FindAsync(VeiculoForm.IdVeiculo);
            if (existing != null)
            {
                existing.DsPlaca = VeiculoForm.DsPlaca;
                existing.IdEstado = VeiculoForm.IdEstado;
                existing.NrANTT = VeiculoForm.NrANTT;
            }
        }

        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var veiculo = await _db.Veiculos.FindAsync(id);
        if (veiculo != null)
        {
            _db.Veiculos.Remove(veiculo);
            await _db.SaveChangesAsync();
        }
        return RedirectToPage();
    }
}
