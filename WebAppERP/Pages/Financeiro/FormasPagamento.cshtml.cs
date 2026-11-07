using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebAppERP.Business.Financeiro;

namespace WebAppERP.Pages.Financeiro;

// Tela de Formas de Pagamento: so conversa com a tela. As regras ficam em FormaPagamentoBusiness.
public class FormasPagamentoModel : PaginaBase
{
    private readonly FormaPagamentoBusiness _forma;

    public FormasPagamentoModel(FormaPagamentoBusiness forma) => _forma = forma;

    public List<FormaPagamento> Formas { get; set; } = [];

    [BindProperty]
    public FormaPagamento FormaForm { get; set; } = new();

    public async Task OnGet()
    {
        Formas = await _forma.Listar();
    }

    public Task<IActionResult> OnPost() =>
        Executar(() => _forma.Salvar(FormaForm), FormaForm.IdFormaPagamento == 0 ? "Forma de pagamento cadastrada com sucesso." : "Forma de pagamento atualizada com sucesso.");

    public Task<IActionResult> OnPostAlterarSituacao(int id) =>
        ExecutarComMensagem(() => _forma.AlterarSituacao(id));

    public Task<IActionResult> OnPostExcluir(int id) =>
        Executar(() => _forma.Excluir(id), "Forma de pagamento excluída com sucesso.");

    // Cadastro pelo componente em modo AJAX (pesquisa de forma de outras
    // telas). Mesmas regras do OnPost, mas responde JSON para a tela de origem.
    public Task<IActionResult> OnPostSalvarAjax() =>
        ExecutarJson(async () =>
        {
            var f = await _forma.Salvar(FormaForm);
            return new { sucesso = true, id = f.IdFormaPagamento, nome = f.DsFormaPagamento, ativo = f.FlAtivo };
        });

    // Pesquisa (componente): por codigo ou descricao. incluirInativos: so as inativas.
    public async Task<IActionResult> OnGetBuscarFormas(int? id, string? termo, bool incluirInativos = false)
    {
        var lista = await _forma.Pesquisar(id, termo, incluirInativos);
        return new JsonResult(lista.Select(f => new { id = f.IdFormaPagamento, registro = Module.Registro(f), nome = f.DsFormaPagamento, ativo = f.FlAtivo }));
    }
}
