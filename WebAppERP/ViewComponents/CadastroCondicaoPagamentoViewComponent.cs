using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;
using WebAppERP.Models;
using WebAppERP.ViewModels;

namespace WebAppERP.ViewComponents;

// Modal de cadastro de condicao de pagamento (com a grade de parcelas),
// reaproveitado pela tela de Condicoes de Pagamento e pelo + do cadastro de
// fornecedor. Traz o proprio script (wwwroot/js/cadastro-condicao.js).
// View: Pages/Shared/Components/CadastroCondicaoPagamento/Default.cshtml.
// A gravacao fica em CondicoesPagamentoModel (OnPostAsync / OnPostSalvarAjaxAsync).
public class CadastroCondicaoPagamentoViewComponent : ViewComponent
{
    private const string ChaveScriptIncluido = "CadastroCondicaoPagamento.ScriptIncluido";
    private readonly AppDbContext _db;

    public CadastroCondicaoPagamentoViewComponent(AppDbContext db) => _db = db;

    public async Task<IViewComponentResult> InvokeAsync(string modo = "post", FinCondicaoPagamento? form = null)
    {
        var vm = new CadastroCondicaoPagamentoViewModel
        {
            CondicaoForm = form ?? new FinCondicaoPagamento(),
            Modo = modo == "ajax" ? "ajax" : "post",
            Formas = await _db.FormasPagamento.Where(f => f.FlAtivo).OrderBy(f => f.DsFormaPagamento).ToListAsync()
        };

        if (!HttpContext.Items.ContainsKey(ChaveScriptIncluido))
        {
            HttpContext.Items[ChaveScriptIncluido] = true;
            vm.IncluirScript = true;
        }

        return View(vm);
    }
}
