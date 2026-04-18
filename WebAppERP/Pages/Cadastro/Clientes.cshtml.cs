using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;
using WebAppERP.Models;

namespace WebAppERP.Pages.Cadastro;

public class ClientesModel : PageModel
{
    private readonly AppDbContext _db;

    public ClientesModel(AppDbContext db) => _db = db;

    public List<GerCliente> Clientes { get; set; } = [];
    public List<GerCidade> Cidades { get; set; } = [];

    [BindProperty]
    public GerCliente ClienteForm { get; set; } = new();

    public async Task OnGetAsync()
    {
        Clientes = await _db.Clientes
            .Include(c => c.Cidade)
            .ThenInclude(c => c!.Estado)
            .OrderBy(c => c.NmCliente)
            .ToListAsync();

        Cidades = await _db.Cidades.Include(c => c.Estado).OrderBy(c => c.NmCidade).ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (ClienteForm.IdCliente == 0)
        {
            ClienteForm.DtCriacao = DateTime.Now;
            _db.Clientes.Add(ClienteForm);
        }
        else
        {
            var existing = await _db.Clientes.FindAsync(ClienteForm.IdCliente);
            if (existing != null)
            {
                existing.NmCliente = ClienteForm.NmCliente;
                existing.NrCpf = ClienteForm.NrCpf;
                existing.NrTelefone = ClienteForm.NrTelefone;
                existing.DsEmail = ClienteForm.DsEmail;
                existing.DsEndereco = ClienteForm.DsEndereco;
                existing.DsBairro = ClienteForm.DsBairro;
                existing.IdCidade = ClienteForm.IdCidade;
                existing.NrCEP = ClienteForm.NrCEP;
                existing.DtNascimento = ClienteForm.DtNascimento;
                existing.DsSexo = ClienteForm.DsSexo;
                existing.DsObservacao = ClienteForm.DsObservacao;
                existing.FlAtivo = ClienteForm.FlAtivo;
                existing.DtEdicao = DateTime.Now;
            }
        }

        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var cliente = await _db.Clientes.FindAsync(id);
        if (cliente != null)
        {
            _db.Clientes.Remove(cliente);
            await _db.SaveChangesAsync();
        }
        return RedirectToPage();
    }
}
