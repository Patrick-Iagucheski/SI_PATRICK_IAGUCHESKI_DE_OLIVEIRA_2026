using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;
using WebAppERP.Models;

namespace WebAppERP.Pages.Cadastro;

public class CategoriasModel : PageModel
{
    private readonly AppDbContext _db;

    public CategoriasModel(AppDbContext db) => _db = db;

    public List<EstCategoria> Categorias { get; set; } = [];

    [BindProperty]
    public EstCategoria CategoriaForm { get; set; } = new();

    public async Task OnGetAsync()
    {
        // Por padrao a listagem mostra apenas ativas (checkbox de inativas desmarcada).
        Categorias = await _db.Categorias
            .Where(c => c.FlAtivo)
            .OrderBy(c => c.NmCategoria)
            .ToListAsync();
    }

    // Busca de categorias via AJAX (filtros da pagina)
    // Pesquisa por: codigo ou nome

    public async Task<IActionResult> OnGetBuscarCategoriasAsync(int? id, string? termo, bool incluirInativos = false)
    {
        var query = _db.Categorias.AsQueryable();

        // Checkbox desmarcada => apenas ativas. Marcada => apenas inativas.
        if (incluirInativos)
            query = query.Where(c => !c.FlAtivo);
        else
            query = query.Where(c => c.FlAtivo);

        if (id.HasValue)
            query = query.Where(c => c.IdCategoria == id.Value);

        if (!string.IsNullOrWhiteSpace(termo))
        {
            var t = termo.Trim();
            query = query.Where(c => c.NmCategoria.Contains(t));
        }

        var lista = await query
            .OrderBy(c => c.NmCategoria)
            .Take(100)
            .Select(c => new
            {
                id = c.IdCategoria,
                nome = c.NmCategoria,
                ativo = c.FlAtivo
            })
            .ToListAsync();

        return new JsonResult(lista);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        //  Validacoes
        if (string.IsNullOrWhiteSpace(CategoriaForm.NmCategoria))
        {
            TempData["Erro"] = "O nome da categoria é obrigatório.";
            return RedirectToPage();
        }

        // Nome unico (respeita a constraint UQ_tbEstCategorias_nmCategoria)
        var nome = CategoriaForm.NmCategoria.Trim();
        bool nomeExiste = await _db.Categorias.AnyAsync(c =>
            c.NmCategoria == nome && c.IdCategoria != CategoriaForm.IdCategoria);
        if (nomeExiste)
        {
            TempData["Erro"] = $"Já existe uma categoria chamada \"{nome}\".";
            return RedirectToPage();
        }

        // --- Persistencia (upsert, igual ao padrao do projeto) ---
        if (CategoriaForm.IdCategoria == 0)
        {
            CategoriaForm.NmCategoria = nome;
            CategoriaForm.DtCriacao = DateTime.Now;
            _db.Categorias.Add(CategoriaForm);
            TempData["Sucesso"] = "Categoria cadastrada com sucesso.";
        }
        else
        {
            var existing = await _db.Categorias.FindAsync(CategoriaForm.IdCategoria);
            if (existing != null)
            {
                existing.NmCategoria = nome;
                existing.FlAtivo = CategoriaForm.FlAtivo;
                existing.DtEdicao = DateTime.Now;
                TempData["Sucesso"] = "Categoria atualizada com sucesso.";
            }
        }

        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var categoria = await _db.Categorias.FindAsync(id);
        if (categoria == null)
            return RedirectToPage();

        // A FK FK_tbEstProdutos_Categoria impede a exclusao de categoria em uso.
        // Avisa em vez de estourar a excecao do banco na cara do usuario.
        bool emUso = await _db.Produtos.AnyAsync(p => p.IdCategoria == id);
        if (emUso)
        {
            TempData["Erro"] = "Esta categoria está vinculada a produtos e não pode ser excluída. Inative-a.";
            return RedirectToPage();
        }

        _db.Categorias.Remove(categoria);
        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Categoria excluída com sucesso.";
        return RedirectToPage();
    }
}
