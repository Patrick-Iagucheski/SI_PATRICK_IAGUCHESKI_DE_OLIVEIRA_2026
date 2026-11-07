using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebAppERP.Business.Gerais;

namespace WebAppERP.Pages.Gerais;

// Tela de Cargos: so conversa com a tela. As regras ficam em CargoBusiness.
public class CargosModel : PaginaBase
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
            id = c.IdCargo, registro = Module.Registro(c),
            nome = c.NmCargo,
            desc = c.DsCargo,
            comissao = c.VlComissaoPadrao,
            ativo = c.FlAtivo
        }));
    }

    public Task<IActionResult> OnPost() =>
        Executar(() => _cargo.Salvar(CargoForm), CargoForm.IdCargo == 0 ? "Cargo cadastrado com sucesso." : "Cargo atualizado com sucesso.");

    public Task<IActionResult> OnPostExcluir(int id) =>
        Executar(() => _cargo.Excluir(id), "Cargo excluído com sucesso.");
}
