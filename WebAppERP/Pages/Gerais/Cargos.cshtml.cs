using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebAppERP.Business.Gerais;

namespace WebAppERP.Pages.Gerais;

// Tela de Cargos: so conversa com a tela. As regras ficam em CargoBusiness.
public class CargosModel : PageModel
{
    private readonly CargoBusiness _cargo;

    public CargosModel(CargoBusiness cargo) => _cargo = cargo;

    public List<Cargo> Cargos { get; set; } = [];

    [BindProperty]
    public Cargo CargoForm { get; set; } = new();

    public async Task OnGet()
    {
        // Por padrao a listagem mostra apenas ativos (checkbox de inativos desmarcada).
        Cargos = await _cargo.Listar();
    }

    // Busca de cargos via AJAX (filtros da pagina): por codigo ou nome/descricao.
    public async Task<IActionResult> OnGetBuscarCargos(int? id, string? termo, bool incluirInativos = false)
    {
        var lista = await _cargo.Buscar(id, termo, incluirInativos);
        return new JsonResult(lista.Select(c => new
        {
            id = c.IdCargo,
            nome = c.NmCargo,
            desc = c.DsCargo,
            comissao = c.VlComissaoPadrao,
            ativo = c.FlAtivo
        }));
    }

    public async Task<IActionResult> OnPost()
    {
        try
        {
            var novo = CargoForm.IdCargo == 0;
            await _cargo.Salvar(CargoForm);
            TempData["Sucesso"] = novo ? "Cargo cadastrado com sucesso." : "Cargo atualizado com sucesso.";
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
            await _cargo.Excluir(id);
            TempData["Sucesso"] = "Cargo excluído com sucesso.";
        }
        catch (ValidacaoException ex)
        {
            TempData["Erro"] = ex.MensagemCompleta;
        }
        return RedirectToPage();
    }
}
