using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;
using WebAppERP.Models;

namespace WebAppERP.Pages.Cadastro;

public class CidadesModel : PageModel
{
    private readonly AppDbContext _db;

    public CidadesModel(AppDbContext db) => _db = db;

    public List<GerCidade> Cidades { get; set; } = [];
    public List<GerEstado> Estados { get; set; } = [];

    [BindProperty]
    public GerCidade CidadeForm { get; set; } = new();

    public async Task OnGetAsync()
    {
        Cidades = await _db.Cidades
            .Include(c => c.Estado)
            .OrderBy(c => c.NmCidade)
            .ToListAsync();

        Estados = await _db.Estados.OrderBy(e => e.NmEstado).ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (CidadeForm.IdCidade == 0)
        {
            CidadeForm.DtCriacao = DateTime.Now;
            _db.Cidades.Add(CidadeForm);
        }
        else
        {
            var existing = await _db.Cidades.FindAsync(CidadeForm.IdCidade);
            if (existing != null)
            {
                existing.NmCidade = CidadeForm.NmCidade;
                existing.IdEstado = CidadeForm.IdEstado;
                existing.DtEdicao = DateTime.Now;
            }
        }

        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var cidade = await _db.Cidades.FindAsync(id);
        if (cidade != null)
        {
            _db.Cidades.Remove(cidade);
            await _db.SaveChangesAsync();
        }
        return RedirectToPage();
    }
}
