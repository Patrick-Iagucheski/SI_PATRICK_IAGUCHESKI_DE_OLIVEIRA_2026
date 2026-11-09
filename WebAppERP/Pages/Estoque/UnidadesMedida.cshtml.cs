using Microsoft.AspNetCore.Mvc;
using WebAppERP.Business.Estoque;

namespace WebAppERP.Pages.Estoque;

// Tela de Unidades de Medida: so conversa com a tela. As regras ficam em
// UnidadeMedidaBusiness.
public class UnidadesMedidaModel : PaginaBase
{
    private readonly UnidadeMedidaBusiness _unidade;

    public UnidadesMedidaModel(UnidadeMedidaBusiness unidade) => _unidade = unidade;

    public List<UnidadeMedida> Unidades { get; set; } = [];

    [BindProperty]
    public UnidadeMedida UnidadeForm { get; set; } = new();

    public async Task OnGet()
    {
        // Por padrao a listagem mostra apenas ativas (checkbox de inativas desmarcada).
        Unidades = await _unidade.Listar();
    }

    // Unidade no formato que a tela e a pesquisa usam (editar, grade e Registro).
    public static object Registro(UnidadeMedida u) => new
    {
        id = u.IdUnidadeMedida,
        registro = Module.Registro(u),
        sigla = u.SgUnidade,
        nome = u.DsUnidade,
        descricao = u.Descricao,
        ativo = u.FlAtivo
    };

    // Busca via AJAX (filtros da tela e pesquisa do campo de unidade do produto).
    public async Task<IActionResult> OnGetBuscarUnidades(int? id, string? termo, bool incluirInativos = false)
    {
        var lista = await _unidade.Buscar(id, termo, incluirInativos);
        return new JsonResult(lista.Select(Registro));
    }

    public Task<IActionResult> OnPost() =>
        Executar(() => _unidade.Salvar(UnidadeForm),
            UnidadeForm.IdUnidadeMedida == 0 ? "Unidade de medida cadastrada com sucesso." : "Unidade de medida atualizada com sucesso.");

    public Task<IActionResult> OnPostExcluir(int id) =>
        Executar(() => _unidade.Excluir(id), "Unidade de medida excluída com sucesso.");
}
