using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebAppERP.Business.Gerais;

namespace WebAppERP.Pages.Gerais;

// Tela de Paises: so conversa com a tela. As regras ficam em PaisBusiness.
public class PaisesModel : PaginaBase
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

    public Task<IActionResult> OnPost() =>
        Executar(() => _pais.Salvar(PaisForm), PaisForm.IdPais == 0 ? "País cadastrado com sucesso." : "País atualizado com sucesso.");

    public Task<IActionResult> OnPostExcluir(int id) =>
        Executar(() => _pais.Excluir(id), "País excluído com sucesso.");
}
