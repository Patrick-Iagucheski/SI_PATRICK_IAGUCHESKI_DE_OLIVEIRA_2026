using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebAppERP.Business.Estoque;

namespace WebAppERP.Pages.Estoque;

// Tela de Marcas: so conversa com a tela. As regras ficam em MarcaBusiness.
public class MarcasModel : PageModel
{
    private readonly MarcaBusiness _marca;

    public MarcasModel(MarcaBusiness marca) => _marca = marca;

    public List<Marca> Marcas { get; set; } = [];

    [BindProperty]
    public Marca MarcaForm { get; set; } = new();

    public async Task OnGet()
    {
        // Por padrao a listagem mostra apenas ativas (checkbox de inativas desmarcada).
        Marcas = await _marca.Listar();
    }

    // Busca de marcas via AJAX (filtros da pagina): por codigo ou nome.
    public async Task<IActionResult> OnGetBuscarMarcas(int? id, string? termo, bool incluirInativos = false)
    {
        var lista = await _marca.Buscar(id, termo, incluirInativos);
        return new JsonResult(lista.Select(m => new
        {
            id = m.IdMarca,
            nome = m.NmMarca,
            ativo = m.FlAtivo
        }));
    }

    public async Task<IActionResult> OnPost()
    {
        try
        {
            var nova = MarcaForm.IdMarca == 0;
            await _marca.Salvar(MarcaForm);
            TempData["Sucesso"] = nova ? "Marca cadastrada com sucesso." : "Marca atualizada com sucesso.";
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
            await _marca.Excluir(id);
            TempData["Sucesso"] = "Marca excluída com sucesso.";
        }
        catch (ValidacaoException ex)
        {
            TempData["Erro"] = ex.MensagemCompleta;
        }
        return RedirectToPage();
    }
}
