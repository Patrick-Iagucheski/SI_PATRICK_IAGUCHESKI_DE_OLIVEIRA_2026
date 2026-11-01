using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebAppERP.Business.Gerais;

namespace WebAppERP.Pages.Gerais;

// Tela de Estados: so conversa com a tela. As regras ficam em EstadoBusiness.
public class EstadosModel : PageModel
{
    private readonly EstadoBusiness _estado;
    private readonly PaisBusiness _pais;

    public EstadosModel(EstadoBusiness estado, PaisBusiness pais)
    {
        _estado = estado;
        _pais = pais;
    }

    public List<Estado> Estados { get; set; } = [];
    public List<Pais> Paises { get; set; } = [];

    [BindProperty]
    public Estado EstadoForm { get; set; } = new();

    public async Task OnGet()
    {
        Estados = await _estado.Listar();
        Paises = await _pais.Listar();
    }

    public async Task<IActionResult> OnPost()
    {
        try
        {
            var novo = EstadoForm.IdEstado == 0;
            await _estado.Salvar(EstadoForm);
            TempData["Sucesso"] = novo ? "Estado cadastrado com sucesso." : "Estado atualizado com sucesso.";
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
            await _estado.Excluir(id);
            TempData["Sucesso"] = "Estado excluído com sucesso.";
        }
        catch (ValidacaoException ex)
        {
            TempData["Erro"] = ex.MensagemCompleta;
        }
        return RedirectToPage();
    }
}
