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

    // Dados reais para os graficos
    public List<string> FaturamentoSemanaLabels { get; set; } = [];
    public List<decimal> FaturamentoSemanaValores { get; set; } = [];
    public List<string> ServicosLabels { get; set; } = [];
    public List<int> ServicosValores { get; set; } = [];

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

        await CarregarGraficosAsync(hoje, inicioMes);
    }

    private async Task CarregarGraficosAsync(DateOnly hoje, DateOnly inicioMes)
    {
        // Faturamento dos ultimos 7 dias (vendas finalizadas), agrupado por dia
        var inicioSemana = hoje.AddDays(-6);
        var inicioSemanaDt = inicioSemana.ToDateTime(TimeOnly.MinValue);
        var fimSemanaDt = hoje.ToDateTime(TimeOnly.MaxValue);

        var vendasSemana = await _db.Vendas
            .Where(v => v.TpStatus == "FINALIZADA" && v.DtVenda >= inicioSemanaDt && v.DtVenda <= fimSemanaDt)
            .Select(v => new { v.DtVenda, v.VlTotal })
            .ToListAsync();

        var porDia = vendasSemana
            .GroupBy(v => DateOnly.FromDateTime(v.DtVenda))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.VlTotal));

        string[] diasSemana = ["Dom", "Seg", "Ter", "Qua", "Qui", "Sex", "Sab"];
        for (var dia = inicioSemana; dia <= hoje; dia = dia.AddDays(1))
        {
            FaturamentoSemanaLabels.Add(diasSemana[(int)dia.DayOfWeek]);
            FaturamentoSemanaValores.Add(porDia.TryGetValue(dia, out var total) ? total : 0m);
        }

        // Servicos mais solicitados no mes (top 5 por quantidade de agendamentos)
        var servicosTop = await _db.Agendamentos
            .Where(a => a.DtAgendamento >= inicioMes && a.DtAgendamento <= hoje
                        && a.TpStatus != "CANCELADO" && a.Servico != null)
            .GroupBy(a => a.Servico!.DsServico)
            .Select(g => new { Servico = g.Key, Quantidade = g.Count() })
            .OrderByDescending(g => g.Quantidade)
            .Take(5)
            .ToListAsync();

        foreach (var item in servicosTop)
        {
            ServicosLabels.Add(item.Servico);
            ServicosValores.Add(item.Quantidade);
        }
    }
}
