using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;
using WebAppERP.Models;

namespace WebAppERP.Pages.Cadastro;

public class TransportadoresModel : PageModel
{
    private readonly AppDbContext _db;

    public TransportadoresModel(AppDbContext db) => _db = db;

    public List<GerTransportador> Transportadores { get; set; } = [];
    public List<GerCidade> Cidades { get; set; } = [];

    [BindProperty]
    public GerTransportador TransportadorForm { get; set; } = new();

    public async Task OnGetAsync()
    {
        Transportadores = await _db.Transportadores
            .Include(t => t.Cidade)
            .ThenInclude(c => c!.Estado)
            .OrderBy(t => t.DsRazaoSocial)
            .ToListAsync();

        Cidades = await _db.Cidades.Include(c => c.Estado).OrderBy(c => c.NmCidade).ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (TransportadorForm.IdTransportador == 0)
        {
            _db.Transportadores.Add(TransportadorForm);
        }
        else
        {
            var existing = await _db.Transportadores.FindAsync(TransportadorForm.IdTransportador);
            if (existing != null)
            {
                existing.DsRazaoSocial = TransportadorForm.DsRazaoSocial;
                existing.NrCpfCnpj = TransportadorForm.NrCpfCnpj;
                existing.DsEndereco = TransportadorForm.DsEndereco;
                existing.IdCidade = TransportadorForm.IdCidade;
                existing.NrInscEstadual = TransportadorForm.NrInscEstadual;
            }
        }

        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var transportador = await _db.Transportadores.FindAsync(id);
        if (transportador != null)
        {
            _db.Transportadores.Remove(transportador);
            await _db.SaveChangesAsync();
        }
        return RedirectToPage();
    }
}
