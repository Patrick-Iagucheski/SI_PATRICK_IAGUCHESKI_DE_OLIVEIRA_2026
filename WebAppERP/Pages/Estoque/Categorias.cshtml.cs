using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebAppERP.Business.Estoque;

namespace WebAppERP.Pages.Estoque;

// Tela de Categorias: so conversa com a tela. As regras ficam em CategoriaBusiness.
public class CategoriasModel : PageModel
{
    private readonly CategoriaBusiness _categoria;

    public CategoriasModel(CategoriaBusiness categoria) => _categoria = categoria;

    public List<Categoria> Categorias { get; set; } = [];

    [BindProperty]
    public Categoria CategoriaForm { get; set; } = new();

    public async Task OnGet()
    {
        // Por padrao a listagem mostra apenas ativas (checkbox de inativas desmarcada).
        Categorias = await _categoria.Listar();
    }

    // Busca de categorias via AJAX (filtros da pagina): por codigo ou nome.
    public async Task<IActionResult> OnGetBuscarCategorias(int? id, string? termo, bool incluirInativos = false)
    {
        var lista = await _categoria.Buscar(id, termo, incluirInativos);
        return new JsonResult(lista.Select(c => new
        {
            id = c.IdCategoria,
            nome = c.NmCategoria,
            ativo = c.FlAtivo
        }));
    }

    public async Task<IActionResult> OnPost()
    {
        try
        {
            var nova = CategoriaForm.IdCategoria == 0;
            await _categoria.Salvar(CategoriaForm);
            TempData["Sucesso"] = nova ? "Categoria cadastrada com sucesso." : "Categoria atualizada com sucesso.";
        }
        catch (ValidacaoException ex)
        {
            TempData["Erro"] = ex.MensagemCompleta;
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostExcluir(int id)
    {
        try
        {
            await _categoria.Excluir(id);
            TempData["Sucesso"] = "Categoria excluída com sucesso.";
        }
        catch (ValidacaoException ex)
        {
            TempData["Erro"] = ex.MensagemCompleta;
        }
        return RedirectToPage();
    }
}
