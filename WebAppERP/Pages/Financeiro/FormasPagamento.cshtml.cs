using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebAppERP.Business.Financeiro;

namespace WebAppERP.Pages.Financeiro;

// Tela de Formas de Pagamento: so conversa com a tela. As regras ficam em FormaPagamentoBusiness.
public class FormasPagamentoModel : PageModel
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

    public async Task<IActionResult> OnPost()
    {
        try
        {
            var nova = FormaForm.IdFormaPagamento == 0;
            await _forma.Salvar(FormaForm);
            TempData["Sucesso"] = nova ? "Forma de pagamento cadastrada com sucesso." : "Forma de pagamento atualizada com sucesso.";
        }
        catch (ValidacaoException ex)
        {
            TempData["Erro"] = ex.MensagemCompleta;
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAlterarSituacao(int id)
    {
        var ativa = await _forma.AlterarSituacao(id);
        if (ativa != null)
            TempData["Sucesso"] = ativa.Value ? "Forma de pagamento ativada." : "Forma de pagamento inativada.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostExcluir(int id)
    {
        try
        {
            await _forma.Excluir(id);
            TempData["Sucesso"] = "Forma de pagamento excluída com sucesso.";
        }
        catch (ValidacaoException ex)
        {
            TempData["Erro"] = ex.MensagemCompleta;
        }
        return RedirectToPage();
    }
}
