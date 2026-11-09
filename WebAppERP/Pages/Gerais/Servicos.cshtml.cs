using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebAppERP.Business.Gerais;

namespace WebAppERP.Pages.Gerais;

// Tela de Servicos: so conversa com a tela. As regras ficam em ServicoBusiness.
public class ServicosModel : PaginaBase
{
    private readonly ServicoBusiness _servico;
    private readonly CategoriaServicoBusiness _categoria;

    public ServicosModel(ServicoBusiness servico, CategoriaServicoBusiness categoria)
    {
        _servico = servico;
        _categoria = categoria;
    }

    public List<Servico> Servicos { get; set; } = [];

    [BindProperty]
    public Servico ServicoForm { get; set; } = new();

    public async Task OnGet()
    {
        Servicos = await _servico.Listar();
    }

    public Task<IActionResult> OnPost() =>
        Executar(() => _servico.Salvar(ServicoForm), ServicoForm.IdServico == 0 ? "Serviço cadastrado com sucesso." : "Serviço atualizado com sucesso.");

    public Task<IActionResult> OnPostExcluir(int id) =>
        Executar(() => _servico.Excluir(id), "Serviço excluído com sucesso.");

    // Cadastro pelo componente em modo AJAX (pesquisas de servico de outras
    // telas). Mesmas regras do OnPost, mas responde JSON para a tela de origem.
    public Task<IActionResult> OnPostSalvarAjax() =>
        ExecutarJson(async () =>
        {
            var s = await _servico.Salvar(ServicoForm);
            return new { sucesso = true, id = s.IdServico, nome = s.DsServico, valor = s.VlServico, duracao = s.NrDuracaoMin };
        });

    // Cadastro rapido de Categoria de Servico via AJAX (pesquisa do campo de categoria).
    public class CategoriaRapidoDto
    {
        public int IdCategoriaServico { get; set; }
        public string NmCategoria { get; set; } = string.Empty;
    }

    public Task<IActionResult> OnPostCategoriaRapido([FromBody] CategoriaRapidoDto dto) =>
        ExecutarJson(async () =>
        {
            var c = await _categoria.Salvar(new CategoriaServico
            {
                IdCategoriaServico = dto?.IdCategoriaServico ?? 0,
                NmCategoria = dto?.NmCategoria ?? string.Empty,
                FlAtivo = true
            });
            return new { sucesso = true, id = c.IdCategoriaServico, nome = c.NmCategoria };
        });

    // Pesquisa (componente): por codigo, nome ou categoria, com os dados que a
    // grade e o Editar usam. incluirInativos: so os inativos.
    public async Task<IActionResult> OnGetBuscarServicos(int? id, string? termo, bool incluirInativos = false)
    {
        var lista = await _servico.Pesquisar(id, termo, incluirInativos);
        return new JsonResult(lista.Select(s => new
        {
            id = s.IdServico, registro = Module.Registro(s),
            nome = s.DsServico,
            descricao = s.DsDescricao,
            valor = s.VlServico,
            duracao = s.NrDuracaoMin,
            idCategoria = s.IdCategoriaServico,
            categoria = s.CategoriaServico?.NmCategoria,
            ativo = s.FlAtivo
        }));
    }
}
