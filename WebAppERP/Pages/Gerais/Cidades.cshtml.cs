using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebAppERP.Business.Gerais;

namespace WebAppERP.Pages.Gerais;

// Tela de Cidades: so conversa com a tela. As regras ficam em CidadeBusiness.
public class CidadesModel : PaginaBase
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

    public Task<IActionResult> OnPost() =>
        Executar(() => _cidade.Salvar(CidadeForm), CidadeForm.IdCidade == 0 ? "Cidade cadastrada com sucesso." : "Cidade atualizada com sucesso.");

    public Task<IActionResult> OnPostExcluir(int id) =>
        Executar(() => _cidade.Excluir(id), "Cidade excluída com sucesso.");
}
