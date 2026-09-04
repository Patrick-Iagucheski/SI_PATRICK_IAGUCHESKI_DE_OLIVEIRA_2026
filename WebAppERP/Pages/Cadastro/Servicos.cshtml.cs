using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;
using WebAppERP.Models;

namespace WebAppERP.Pages.Cadastro;

public class ServicosModel : PageModel
{
    private readonly AppDbContext _db;

    public ServicosModel(AppDbContext db) => _db = db;

    public List<GerServico> Servicos { get; set; } = [];

    [BindProperty]
    public GerServico ServicoForm { get; set; } = new();

    public async Task OnGetAsync()
    {
        Servicos = await _db.Servicos.OrderBy(s => s.DsServico).ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (ServicoForm.IdServico == 0)
        {
            ServicoForm.DtCriacao = DateTime.Now;
            _db.Servicos.Add(ServicoForm);
        }
        else
        {
            var existing = await _db.Servicos.FindAsync(ServicoForm.IdServico);
            if (existing != null)
            {
                existing.DsServico = ServicoForm.DsServico;
                existing.DsDescricao = ServicoForm.DsDescricao;
                existing.VlServico = ServicoForm.VlServico;
                existing.NrDuracaoMin = ServicoForm.NrDuracaoMin;
                existing.DsCategoria = ServicoForm.DsCategoria;
                existing.FlAtivo = ServicoForm.FlAtivo;
                existing.DtEdicao = DateTime.Now;
            }
        }

        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var servico = await _db.Servicos.FindAsync(id);
        if (servico == null)
            return RedirectToPage();

        // Filhas: tbOpeAgendamentos.idServico e tbOpeVendaItens.idServico.
        if (await _db.Agendamentos.AnyAsync(a => a.IdServico == id))
        {
            TempData["Erro"] = "Não é possível excluir: existe agendamento com este serviço. Inative-o.";
            return RedirectToPage();
        }

        if (await _db.VendaItens.AnyAsync(i => i.IdServico == id))
        {
            TempData["Erro"] = "Não é possível excluir: este serviço consta em uma venda. Inative-o.";
            return RedirectToPage();
        }

        try
        {
            _db.Servicos.Remove(servico);
            await _db.SaveChangesAsync();
            TempData["Sucesso"] = "Serviço excluído com sucesso.";
        }
        catch (DbUpdateException)
        {
            TempData["Erro"] = "Não é possível excluir: existem registros vinculados a este serviço. Inative-o.";
        }
        return RedirectToPage();
    }
}
