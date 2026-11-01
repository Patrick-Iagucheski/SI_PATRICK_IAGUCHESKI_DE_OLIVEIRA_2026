using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebAppERP.Business.Gerais;

namespace WebAppERP.Pages.Gerais;

// Tela de Paises: so conversa com a tela. As regras ficam em PaisBusiness.
public class PaisesModel : PageModel
{
    private readonly PaisBusiness _pais;

    public PaisesModel(PaisBusiness pais) => _pais = pais;

    public List<Pais> Paises { get; set; } = [];

    [BindProperty]
    public Pais PaisForm { get; set; } = new();

    public async Task OnGet()
    {
        Paises = await _pais.Listar();
    }

    public async Task<IActionResult> OnPost()
    {
        try
        {
            var novo = PaisForm.IdPais == 0;
            await _pais.Salvar(PaisForm);
            TempData["Sucesso"] = novo ? "País cadastrado com sucesso." : "País atualizado com sucesso.";
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
            await _pais.Excluir(id);
            TempData["Sucesso"] = "País excluído com sucesso.";
        }
        catch (ValidacaoException ex)
        {
            TempData["Erro"] = ex.MensagemCompleta;
        }
        return RedirectToPage();
    }
}
