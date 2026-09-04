using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;
using WebAppERP.Models;

namespace WebAppERP.Pages.Cadastro;

public class FormasPagamentoModel : PageModel
{
    private readonly AppDbContext _db;

    public FormasPagamentoModel(AppDbContext db) => _db = db;

    public List<FinFormaPagamento> Formas { get; set; } = [];

    [BindProperty]
    public FinFormaPagamento FormaForm { get; set; } = new();

    public async Task OnGetAsync()
    {
        Formas = await _db.FormasPagamento.OrderBy(f => f.DsFormaPagamento).ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (FormaForm.IdFormaPagamento == 0)
        {
            FormaForm.DtCriacao = DateTime.Now;
            _db.FormasPagamento.Add(FormaForm);
        }
        else
        {
            var existing = await _db.FormasPagamento.FindAsync(FormaForm.IdFormaPagamento);
            if (existing != null)
            {
                existing.DsFormaPagamento = FormaForm.DsFormaPagamento;
                existing.FlAtivo = FormaForm.FlAtivo;
            }
        }

        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var forma = await _db.FormasPagamento.FindAsync(id);
        if (forma != null)
        {
            forma.FlAtivo = !forma.FlAtivo;
            await _db.SaveChangesAsync();
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var forma = await _db.FormasPagamento.FindAsync(id);
        if (forma == null)
            return RedirectToPage();

        // Filhas: parcelas de condicao, contas a pagar e vendas (idFormaPagamento).
        if (await _db.CondicoesPagamentoParcelas.AnyAsync(p => p.IdFormaPagamento == id))
        {
            TempData["Erro"] = "Não é possível excluir: esta forma é usada em uma condição de pagamento.";
            return RedirectToPage();
        }

        if (await _db.ContasAPagar.AnyAsync(c => c.IdFormaPagamento == id))
        {
            TempData["Erro"] = "Não é possível excluir: existe conta a pagar com esta forma de pagamento.";
            return RedirectToPage();
        }

        if (await _db.Vendas.AnyAsync(v => v.IdFormaPagamento == id))
        {
            TempData["Erro"] = "Não é possível excluir: existe venda com esta forma de pagamento.";
            return RedirectToPage();
        }

        try
        {
            _db.FormasPagamento.Remove(forma);
            await _db.SaveChangesAsync();
            TempData["Sucesso"] = "Forma de pagamento excluída com sucesso.";
        }
        catch (DbUpdateException)
        {
            TempData["Erro"] = "Não é possível excluir: existem registros vinculados a esta forma de pagamento.";
        }
        return RedirectToPage();
    }
}
