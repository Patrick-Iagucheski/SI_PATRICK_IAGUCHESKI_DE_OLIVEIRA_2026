using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace WebAppERP.Pages;

// Base das telas: o jeito unico de executar uma acao do Business e devolver a
// resposta. Regra nao atendida (ValidacaoException) e erro de gravacao no
// banco (DbUpdateException) viram mensagem para o usuario, entao as telas nao
// repetem try/catch e o usuario nunca ve a pilha tecnica do erro. As mensagens
// de TempData ("Sucesso" e "Erro") sao mostradas pelo _Layout.
public abstract class PaginaBase : PageModel
{
    // Acao de formulario (salvar, excluir, baixar...): grava a mensagem e volta
    // para a tela. rota: valores da rota na volta (ex.: new { DataFiltro = data }).
    // Salvar de modal (enviado por AJAX pelo module.js) responde JSON: no erro o
    // modal mostra a mensagem e continua aberto com o que foi digitado; no
    // sucesso a tela recarrega e mostra a mensagem do TempData.
    protected async Task<IActionResult> Executar(Func<Task> acao, string? sucesso, object? rota = null)
    {
        try
        {
            await acao();
            if (sucesso != null)
                TempData["Sucesso"] = sucesso;
        }
        catch (Exception ex) when (MensagemDeErro(ex) is { } mensagem)
        {
            if (PedidoAjax)
                return new JsonResult(new { sucesso = false, mensagem });
            TempData["Erro"] = mensagem;
        }
        return PedidoAjax ? new JsonResult(new { sucesso = true }) : RedirectToPage(rota);
    }

    // Pedido feito por fetch do module.js (cabecalho X-Requested-With).
    private bool PedidoAjax => Request.Headers["X-Requested-With"] == "XMLHttpRequest";

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
        catch (Exception ex) when (MensagemDeErro(ex) is { } mensagem)
        {
            TempData["Erro"] = mensagem;
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
        catch (Exception ex) when (MensagemDeErro(ex) is { } mensagem)
        {
            return new JsonResult(new { sucesso = false, mensagem });
        }
    }

    // Mensagem para o usuario de um erro da acao, ou null quando o erro e
    // inesperado (defeito no sistema: segue para a pagina de erro). Regra nao
    // atendida mostra a propria mensagem; erro de gravacao no banco que o
    // Business nao previu vira um texto amigavel.
    protected static string? MensagemDeErro(Exception ex) => ex switch
    {
        ValidacaoException v => v.MensagemCompleta,
        DbUpdateException db => MensagemDoBanco(db),
        _ => null
    };

    private static string MensagemDoBanco(DbUpdateException ex)
    {
        if (ex.InnerException is SqlException sql)
        {
            // 2601/2627: indice unico ou chave duplicada
            if (sql.Number is 2601 or 2627)
                return $"Já existe um cadastro com {CampoDuplicado(sql.Message)}. Verifique os dados informados.";
            // 547: chave estrangeira (registro ligado a outro cadastro)
            if (sql.Number == 547)
                return "Não foi possível gravar: o registro está ligado a outro cadastro. Verifique os dados informados.";
            // 8115: estouro aritmetico (numero maior que a coluna aceita)
            if (sql.Number == 8115)
                return "Um dos valores informados é maior que o permitido. Verifique os números digitados.";
            // 8152 / 2628: texto maior que a coluna aceita
            if (sql.Number is 8152 or 2628)
                return "Um dos textos informados é maior que o permitido. Verifique os campos preenchidos.";
        }
        // Numero que nem cabe no parametro (o SqlClient recusa antes de enviar)
        if (ex.InnerException is ArgumentException)
            return "Um dos valores informados é maior que o permitido. Verifique os números digitados.";
        return "Não foi possível gravar os dados. Verifique as informações e tente novamente.";
    }

    // Campo pelo nome do indice unico que recusou a gravacao
    // (UQ_tbGerClientes_nrCpf -> "este CPF").
    private static string CampoDuplicado(string mensagemSql)
    {
        var indice = Regex.Match(mensagemSql, @"'(UQ_\w+)'").Groups[1].Value;
        var coluna = indice[(indice.LastIndexOf('_') + 1)..];
        return coluna switch
        {
            "nrCpf" => "este CPF",
            "nrCnpj" => "este CNPJ",
            "dsPlaca" => "esta placa",
            "dsLogin" => "este login",
            _ when coluna.StartsWith("nm") || coluna.StartsWith("ds") || coluna == "Estado" => "este nome",
            _ => "estes dados"
        };
    }
}
