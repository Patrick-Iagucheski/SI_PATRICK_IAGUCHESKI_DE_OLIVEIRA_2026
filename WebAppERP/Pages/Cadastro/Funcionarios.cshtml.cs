using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;
using WebAppERP.Models;

namespace WebAppERP.Pages.Cadastro;

public class FuncionariosModel : PageModel
{
    private readonly AppDbContext _db;

    public FuncionariosModel(AppDbContext db) => _db = db;

    public List<GerFuncionario> Funcionarios { get; set; } = [];
    public List<GerCidade> Cidades { get; set; } = [];

    [BindProperty]
    public GerFuncionario FuncionarioForm { get; set; } = new();

    public async Task OnGetAsync()
    {
        Funcionarios = await _db.Funcionarios
            .Include(f => f.Cidade)
            .ThenInclude(c => c!.Estado)
            .OrderBy(f => f.NmFuncionario)
            .ToListAsync();

        Cidades = await _db.Cidades.Include(c => c.Estado).OrderBy(c => c.NmCidade).ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (FuncionarioForm.IdFuncionario == 0)
        {
            FuncionarioForm.DtCriacao = DateTime.Now;
            _db.Funcionarios.Add(FuncionarioForm);
        }
        else
        {
            var existing = await _db.Funcionarios.FindAsync(FuncionarioForm.IdFuncionario);
            if (existing != null)
            {
                existing.NmFuncionario = FuncionarioForm.NmFuncionario;
                existing.NrCpf = FuncionarioForm.NrCpf;
                existing.NrTelefone = FuncionarioForm.NrTelefone;
                existing.DsEmail = FuncionarioForm.DsEmail;
                existing.DsEndereco = FuncionarioForm.DsEndereco;
                existing.DsBairro = FuncionarioForm.DsBairro;
                existing.IdCidade = FuncionarioForm.IdCidade;
                existing.NrCEP = FuncionarioForm.NrCEP;
                existing.DtNascimento = FuncionarioForm.DtNascimento;
                existing.DtAdmissao = FuncionarioForm.DtAdmissao;
                existing.DtDemissao = FuncionarioForm.DtDemissao;
                existing.DsCargo = FuncionarioForm.DsCargo;
                existing.VlSalario = FuncionarioForm.VlSalario;
                existing.VlComissao = FuncionarioForm.VlComissao;
                existing.DsObservacao = FuncionarioForm.DsObservacao;
                existing.FlAtivo = FuncionarioForm.FlAtivo;
                existing.DtEdicao = DateTime.Now;
            }
        }

        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var func = await _db.Funcionarios.FindAsync(id);
        if (func != null)
        {
            _db.Funcionarios.Remove(func);
            await _db.SaveChangesAsync();
        }
        return RedirectToPage();
    }
}
