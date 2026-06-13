using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;
using WebAppERP.Models;

namespace WebAppERP.Pages.Cadastro;

public class FornecedoresModel : PageModel
{
    private readonly AppDbContext _db;

    public FornecedoresModel(AppDbContext db) => _db = db;

    public List<GerFornecedor> Fornecedores { get; set; } = [];
    public List<GerCidade> Cidades { get; set; } = [];

    [BindProperty]
    public GerFornecedor FornecedorForm { get; set; } = new();

    public async Task OnGetAsync()
    {
        Fornecedores = await _db.Fornecedores
            .Include(f => f.Cidade)
            .ThenInclude(c => c!.Estado)
            .OrderBy(f => f.DsRazaoSocial)
            .ToListAsync();

        Cidades = await _db.Cidades.Include(c => c.Estado).OrderBy(c => c.NmCidade).ToListAsync();
    }

    private static string? SomenteNumeros(string? valor) =>
        string.IsNullOrEmpty(valor) ? valor : new string(valor.Where(char.IsDigit).ToArray());

    public async Task<IActionResult> OnPostAsync()
    {
        // Remove a máscara antes de salvar (banco guarda só os números)
        FornecedorForm.NrCNPJ = SomenteNumeros(FornecedorForm.NrCNPJ) ?? string.Empty;
        FornecedorForm.NrCEP = SomenteNumeros(FornecedorForm.NrCEP);
        FornecedorForm.NrTelefone = SomenteNumeros(FornecedorForm.NrTelefone);

        if (FornecedorForm.IdFornecedor == 0)
        {
            FornecedorForm.DtCriacao = DateTime.Now;
            _db.Fornecedores.Add(FornecedorForm);
        }
        else
        {
            var existing = await _db.Fornecedores.FindAsync(FornecedorForm.IdFornecedor);
            if (existing != null)
            {
                existing.DsRazaoSocial = FornecedorForm.DsRazaoSocial;
                existing.DsEndereco = FornecedorForm.DsEndereco;
                existing.DsBairro = FornecedorForm.DsBairro;
                existing.IdCidade = FornecedorForm.IdCidade;
                existing.NrCEP = FornecedorForm.NrCEP;
                existing.NrTelefone = FornecedorForm.NrTelefone;
                existing.DsEmail = FornecedorForm.DsEmail;
                existing.NrInscEstadual = FornecedorForm.NrInscEstadual;
                existing.NrInscEstSubTrib = FornecedorForm.NrInscEstSubTrib;
                existing.NrCNPJ = FornecedorForm.NrCNPJ;
                existing.DtEdicao = DateTime.Now;
            }
        }

        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var fornecedor = await _db.Fornecedores.FindAsync(id);
        if (fornecedor != null)
        {
            _db.Fornecedores.Remove(fornecedor);
            await _db.SaveChangesAsync();
        }
        return RedirectToPage();
    }
}
