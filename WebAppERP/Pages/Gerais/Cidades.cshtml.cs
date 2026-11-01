using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebAppERP.Business.Gerais;

namespace WebAppERP.Pages.Gerais;

// Tela de Cidades: so conversa com a tela. As regras ficam em CidadeBusiness.
public class CidadesModel : PageModel
{
    private readonly CidadeBusiness _cidade;
    private readonly EstadoBusiness _estado;

    public CidadesModel(CidadeBusiness cidade, EstadoBusiness estado)
    {
        _cidade = cidade;
        _estado = estado;
    }

    public List<Cidade> Cidades { get; set; } = [];
    public List<Estado> Estados { get; set; } = [];

    [BindProperty]
    public Cidade CidadeForm { get; set; } = new();

    public async Task OnGet()
    {
        Cidades = await _cidade.Listar();
        Estados = await _estado.Listar();
    }

    public async Task<IActionResult> OnPost()
    {
        try
        {
            var nova = CidadeForm.IdCidade == 0;
            await _cidade.Salvar(CidadeForm);
            TempData["Sucesso"] = nova ? "Cidade cadastrada com sucesso." : "Cidade atualizada com sucesso.";
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
            await _cidade.Excluir(id);
            TempData["Sucesso"] = "Cidade excluída com sucesso.";
        }
        catch (ValidacaoException ex)
        {
            TempData["Erro"] = ex.MensagemCompleta;
        }
        return RedirectToPage();
    }
}
