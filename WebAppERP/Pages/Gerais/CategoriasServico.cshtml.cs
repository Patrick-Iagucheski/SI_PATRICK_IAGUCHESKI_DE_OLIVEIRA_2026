using Microsoft.AspNetCore.Mvc;
using WebAppERP.Business.Gerais;

namespace WebAppERP.Pages.Gerais;

// Tela de Categorias de Servico: so conversa com a tela. As regras ficam em
// CategoriaServicoBusiness.
public class CategoriasServicoModel : PaginaBase
{
    private readonly CategoriaServicoBusiness _categoria;

    public CategoriasServicoModel(CategoriaServicoBusiness categoria) => _categoria = categoria;

    public List<CategoriaServico> Categorias { get; set; } = [];

    [BindProperty]
    public CategoriaServico CategoriaForm { get; set; } = new();

    public async Task OnGet()
    {
        // Por padrao a listagem mostra apenas ativas (checkbox de inativas desmarcada).
        Categorias = await _categoria.Listar();
    }

    // Categoria no formato que a tela e a pesquisa usam (editar, grade e Registro).
    public static object Registro(CategoriaServico c) => new
    {
        id = c.IdCategoriaServico,
        registro = Module.Registro(c),
        nome = c.NmCategoria,
        ativo = c.FlAtivo
    };

    // Busca via AJAX (filtros da tela e pesquisa do campo de categoria do servico).
    public async Task<IActionResult> OnGetBuscarCategorias(int? id, string? termo, bool incluirInativos = false)
    {
        var lista = await _categoria.Buscar(id, termo, incluirInativos);
        return new JsonResult(lista.Select(Registro));
    }

    public Task<IActionResult> OnPost() =>
        Executar(() => _categoria.Salvar(CategoriaForm),
            CategoriaForm.IdCategoriaServico == 0 ? "Categoria de serviço cadastrada com sucesso." : "Categoria de serviço atualizada com sucesso.");

    public Task<IActionResult> OnPostExcluir(int id) =>
        Executar(() => _categoria.Excluir(id), "Categoria de serviço excluída com sucesso.");
}
