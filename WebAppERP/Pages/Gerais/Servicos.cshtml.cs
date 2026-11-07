using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebAppERP.Business.Gerais;

namespace WebAppERP.Pages.Gerais;

// Tela de Servicos: so conversa com a tela. As regras ficam em ServicoBusiness.
public class ServicosModel : PaginaBase
{
    private readonly ServicoBusiness _servico;

    public ServicosModel(ServicoBusiness servico) => _servico = servico;

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

    // Pesquisa (componente): por codigo, nome ou categoria, com os dados que a
    // grade e o Editar usam. incluirInativos: so os inativos.
    public async Task<IActionResult> OnGetBuscarServicos(int? id, string? termo, bool incluirInativos = false)
    {
        var lista = await _servico.Pesquisar(id, termo, incluirInativos);
        return new JsonResult(lista.Select(s => new
        {
            id = s.IdServico,
            nome = s.DsServico,
            descricao = s.DsDescricao,
            valor = s.VlServico,
            duracao = s.NrDuracaoMin,
            categoria = s.DsCategoria,
            ativo = s.FlAtivo
        }));
    }
}
