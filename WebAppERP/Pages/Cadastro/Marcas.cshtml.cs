using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;
using WebAppERP.Models;

namespace WebAppERP.Pages.Cadastro;

public class MarcasModel : PageModel
{
    private readonly AppDbContext _db;

    public MarcasModel(AppDbContext db) => _db = db;

    public List<EstMarca> Marcas { get; set; } = [];

    [BindProperty]
    public EstMarca MarcaForm { get; set; } = new();

    public async Task OnGetAsync()
    {
        // Por padrao a listagem mostra apenas ativas (checkbox de inativas desmarcada).
        Marcas = await _db.Marcas
            .Where(m => m.FlAtivo)
            .OrderBy(m => m.NmMarca)
            .ToListAsync();
    }

    // Busca de marcas via AJAX (filtros da pagina)
    // Pesquisa por: codigo ou nome

    public async Task<IActionResult> OnGetBuscarMarcasAsync(int? id, string? termo, bool incluirInativos = false)
    {
        var query = _db.Marcas.AsQueryable();

        // Checkbox desmarcada => apenas ativas. Marcada => apenas inativas.
        if (incluirInativos)
            query = query.Where(m => !m.FlAtivo);
        else
            query = query.Where(m => m.FlAtivo);

        if (id.HasValue)
            query = query.Where(m => m.IdMarca == id.Value);

        if (!string.IsNullOrWhiteSpace(termo))
        {
            var t = termo.Trim();
            query = query.Where(m => m.NmMarca.Contains(t));
        }

        var lista = await query
            .OrderBy(m => m.NmMarca)
            .Take(100)
            .Select(m => new
            {
                id = m.IdMarca,
                nome = m.NmMarca,
                ativo = m.FlAtivo
            })
            .ToListAsync();

        return new JsonResult(lista);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        //  Validacoes 
        if (string.IsNullOrWhiteSpace(MarcaForm.NmMarca))
        {
            TempData["Erro"] = "O nome da marca é obrigatório.";
            return RedirectToPage();
        }

        // Nome unico (respeita a constraint UQ_tbEstMarcas_nmMarca)
        var nome = MarcaForm.NmMarca.Trim();
        bool nomeExiste = await _db.Marcas.AnyAsync(m =>
            m.NmMarca == nome && m.IdMarca != MarcaForm.IdMarca);
        if (nomeExiste)
        {
            TempData["Erro"] = $"Já existe uma marca chamada \"{nome}\".";
            return RedirectToPage();
        }

        // --- Persistencia (upsert, igual ao padrao do projeto) ---
        if (MarcaForm.IdMarca == 0)
        {
            MarcaForm.NmMarca = nome;
            MarcaForm.DtCriacao = DateTime.Now;
            _db.Marcas.Add(MarcaForm);
            TempData["Sucesso"] = "Marca cadastrada com sucesso.";
        }
        else
        {
            var existing = await _db.Marcas.FindAsync(MarcaForm.IdMarca);
            if (existing != null)
            {
                existing.NmMarca = nome;
                existing.FlAtivo = MarcaForm.FlAtivo;
                existing.DtEdicao = DateTime.Now;
                TempData["Sucesso"] = "Marca atualizada com sucesso.";
            }
        }

        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var marca = await _db.Marcas.FindAsync(id);
        if (marca == null)
            return RedirectToPage();

        // A FK FK_tbEstProdutos_Marca impede excluir marca em uso.
        if (await _db.Produtos.AnyAsync(p => p.IdMarca == id))
        {
            TempData["Erro"] = "Não é possível excluir: existe produto vinculado a esta marca. Inative-a.";
            return RedirectToPage();
        }

        try
        {
            _db.Marcas.Remove(marca);
            await _db.SaveChangesAsync();
            TempData["Sucesso"] = "Marca excluída com sucesso.";
        }
        catch (DbUpdateException)
        {
            TempData["Erro"] = "Não é possível excluir: existem registros vinculados a esta marca. Inative-a.";
        }
        return RedirectToPage();
    }
}
