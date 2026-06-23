using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;
using WebAppERP.Models;

namespace WebAppERP.Pages.Cadastro;

public class CargosModel : PageModel
{
    private readonly AppDbContext _db;

    public CargosModel(AppDbContext db) => _db = db;

    public List<GerCargo> Cargos { get; set; } = [];

    [BindProperty]
    public GerCargo CargoForm { get; set; } = new();

    public async Task OnGetAsync()
    {
        // Por padrao a listagem mostra apenas ativos (checkbox de inativos desmarcada).
        Cargos = await _db.Cargos
            .Where(c => c.FlAtivo)
            .OrderBy(c => c.NmCargo)
            .ToListAsync();
    }

    // ============================================================
    // Busca de cargos via AJAX (filtros da pagina)
    // Pesquisa por: codigo ou nome/descricao
    // ============================================================
    public async Task<IActionResult> OnGetBuscarCargosAsync(int? id, string? termo, bool incluirInativos = false)
    {
        var query = _db.Cargos.AsQueryable();

        // Checkbox desmarcada => apenas ativos. Marcada => apenas inativos.
        if (incluirInativos)
            query = query.Where(c => !c.FlAtivo);
        else
            query = query.Where(c => c.FlAtivo);

        if (id.HasValue)
            query = query.Where(c => c.IdCargo == id.Value);

        if (!string.IsNullOrWhiteSpace(termo))
        {
            var t = termo.Trim();
            query = query.Where(c => c.NmCargo.Contains(t) || (c.DsCargo != null && c.DsCargo.Contains(t)));
        }

        var lista = await query
            .OrderBy(c => c.NmCargo)
            .Take(100)
            .Select(c => new
            {
                id = c.IdCargo,
                nome = c.NmCargo,
                desc = c.DsCargo,
                comissao = c.VlComissaoPadrao,
                ativo = c.FlAtivo
            })
            .ToListAsync();

        return new JsonResult(lista);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // --- Validacoes ---
        if (string.IsNullOrWhiteSpace(CargoForm.NmCargo))
        {
            TempData["Erro"] = "O nome do cargo e obrigatorio.";
            return RedirectToPage();
        }

        // Nome unico (respeita a constraint UQ_tbGerCargos_nmCargo)
        bool nomeExiste = await _db.Cargos.AnyAsync(c =>
            c.NmCargo == CargoForm.NmCargo && c.IdCargo != CargoForm.IdCargo);
        if (nomeExiste)
        {
            TempData["Erro"] = $"Ja existe um cargo chamado \"{CargoForm.NmCargo}\".";
            return RedirectToPage();
        }

        if (CargoForm.VlComissaoPadrao is < 0 or > 999.99m)
        {
            TempData["Erro"] = "Comissao padrao invalida.";
            return RedirectToPage();
        }

        // --- Persistencia (upsert, igual ao padrao do projeto) ---
        if (CargoForm.IdCargo == 0)
        {
            CargoForm.NmCargo = CargoForm.NmCargo.Trim();
            CargoForm.DtCriacao = DateTime.Now;
            _db.Cargos.Add(CargoForm);
            TempData["Sucesso"] = "Cargo cadastrado com sucesso.";
        }
        else
        {
            var existing = await _db.Cargos.FindAsync(CargoForm.IdCargo);
            if (existing != null)
            {
                existing.NmCargo = CargoForm.NmCargo.Trim();
                existing.DsCargo = CargoForm.DsCargo;
                existing.VlComissaoPadrao = CargoForm.VlComissaoPadrao;
                existing.FlAtivo = CargoForm.FlAtivo;
                existing.DtEdicao = DateTime.Now;
                TempData["Sucesso"] = "Cargo atualizado com sucesso.";
            }
        }

        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var cargo = await _db.Cargos.FindAsync(id);
        if (cargo == null)
            return RedirectToPage();

        // Nao deixa excluir um cargo que esta em uso por algum funcionario.
        bool emUso = await _db.Funcionarios.AnyAsync(f => f.IdCargo == id);
        if (emUso)
        {
            TempData["Erro"] = "Nao e possivel excluir: existe funcionario vinculado a este cargo.";
            return RedirectToPage();
        }

        _db.Cargos.Remove(cargo);
        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Cargo excluido com sucesso.";
        return RedirectToPage();
    }
}
