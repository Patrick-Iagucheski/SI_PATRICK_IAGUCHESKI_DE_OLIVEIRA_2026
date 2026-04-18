using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;
using WebAppERP.Models;

namespace WebAppERP.Pages;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db) => _db = db;

    public int TotalAgendamentosHoje { get; set; }
    public decimal FaturamentoMes { get; set; }
    public int TotalClientesAtivos { get; set; }
    public int TotalServicosMes { get; set; }
    public List<OpeAgendamento> ProximosAgendamentos { get; set; } = [];
    public List<OpeVenda> UltimasVendas { get; set; } = [];

    public async Task OnGetAsync()
    {
        var hoje = DateOnly.FromDateTime(DateTime.Today);
        var inicioMes = new DateOnly(hoje.Year, hoje.Month, 1);

        TotalAgendamentosHoje = await _db.Agendamentos
            .CountAsync(a => a.DtAgendamento == hoje && a.TpStatus != "CANCELADO");

        FaturamentoMes = await _db.Vendas
            .Where(v => v.DtVenda.Month == hoje.Month && v.DtVenda.Year == hoje.Year && v.TpStatus == "FINALIZADA")
            .SumAsync(v => v.VlTotal);

        TotalClientesAtivos = await _db.Clientes.CountAsync(c => c.FlAtivo);

        TotalServicosMes = await _db.Agendamentos
            .CountAsync(a => a.DtAgendamento >= inicioMes && a.DtAgendamento <= hoje && a.TpStatus == "CONCLUIDO");

        ProximosAgendamentos = await _db.Agendamentos
            .Include(a => a.Cliente)
            .Include(a => a.Funcionario)
            .Include(a => a.Servico)
            .Where(a => a.DtAgendamento >= hoje && a.TpStatus != "CANCELADO")
            .OrderBy(a => a.DtAgendamento).ThenBy(a => a.HrInicio)
            .Take(5)
            .ToListAsync();

        UltimasVendas = await _db.Vendas
            .Include(v => v.Cliente)
            .OrderByDescending(v => v.DtVenda)
            .Take(5)
            .ToListAsync();
    }
}
