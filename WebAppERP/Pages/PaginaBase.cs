using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebAppERP.Pages;

// Base das telas: o jeito unico de executar uma acao do Business e devolver a
// resposta. Regra nao atendida (ValidacaoException) vira mensagem para o
// usuario, entao as telas nao repetem try/catch. As mensagens de TempData
// ("Sucesso" e "Erro") sao mostradas pelo _Layout.
public abstract class PaginaBase : PageModel
{
    // Acao de formulario (salvar, excluir, baixar...): grava a mensagem e volta
    // para a tela. rota: valores da rota na volta (ex.: new { DataFiltro = data }).
    protected async Task<IActionResult> Executar(Func<Task> acao, string? sucesso, object? rota = null)
    {
        try
        {
            await acao();
            if (sucesso != null)
                TempData["Sucesso"] = sucesso;
        }
        catch (ValidacaoException ex)
        {
            TempData["Erro"] = ex.MensagemCompleta;
        }
        return RedirectToPage(rota);
    }

    // Igual ao Executar, mas a propria acao devolve a mensagem de sucesso
    // (ex.: "Agendamento confirmado."). Mensagem nula ou vazia: nao mostra nada.
    protected async Task<IActionResult> ExecutarComMensagem(Func<Task<string>> acao, object? rota = null)
    {
        try
        {
            var sucesso = await acao();
            if (!string.IsNullOrEmpty(sucesso))
                TempData["Sucesso"] = sucesso;
        }
        catch (ValidacaoException ex)
        {
            TempData["Erro"] = ex.MensagemCompleta;
        }
        return RedirectToPage(rota);
    }

    // Acao chamada por AJAX (cadastro rapido, salvar do componente): devolve o
    // objeto da acao (com sucesso = true) ou { sucesso = false, mensagem }.
    protected async Task<IActionResult> ExecutarJson(Func<Task<object>> acao)
    {
        try
        {
            return new JsonResult(await acao());
        }
        catch (ValidacaoException ex)
        {
            return new JsonResult(new { sucesso = false, mensagem = ex.MensagemCompleta });
        }
    }

    // Salvar de tela com modal grande: no erro, recarrega a tela e reexibe o
    // formulario com o que foi digitado. chaveErro: chave do TempData que a
    // tela usa para reabrir o modal com a mensagem.
    protected async Task<IActionResult> ExecutarOuReexibir(Func<Task> acao, string? sucesso, string chaveErro, Func<Task> recarregar)
    {
        try
        {
            await acao();
            if (sucesso != null)
                TempData["Sucesso"] = sucesso;
            return RedirectToPage();
        }
        catch (ValidacaoException ex)
        {
            TempData[chaveErro] = ex.MensagemCompleta;
            await recarregar();
            return Page();
        }
    }
}
