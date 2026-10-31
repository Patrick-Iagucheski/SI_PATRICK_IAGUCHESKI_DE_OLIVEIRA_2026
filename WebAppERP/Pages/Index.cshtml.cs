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

    // Painel "Atividades no sistema": ultimas vendas, compras e agendamentos
    // criados ou alterados, do mais recente para o mais antigo
    public List<AtividadeSistema> Atividades { get; set; } = [];

    public record AtividadeSistema(
        DateTime Data, string Titulo, string Detalhe, string Icone, string Cor,
        string Pagina, object? Rota, string TituloAba);

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
        await CarregarAtividadesAsync();
    }

    private async Task CarregarAtividadesAsync()
    {
        const int qtd = 10;

        var vendas = await _db.Vendas
            .Include(v => v.Cliente)
            .OrderByDescending(v => v.DtEdicao ?? v.DtCriacao)
            .Take(qtd)
            .ToListAsync();

        var compras = await _db.Compras
            .Include(c => c.Fornecedor)
            .OrderByDescending(c => c.DtEdicao ?? c.DtCriacao)
            .Take(qtd)
            .ToListAsync();

        var agendamentos = await _db.Agendamentos
            .Include(a => a.Cliente)
            .Include(a => a.Servico)
            .OrderByDescending(a => a.DtEdicao ?? a.DtCriacao)
            .Take(qtd)
            .ToListAsync();

        foreach (var v in vendas)
        {
            var acao = v.TpStatus == "CANCELADA" ? "cancelada"
                : v.TpStatus == "FINALIZADA" ? "finalizada"
                : v.DtEdicao != null ? "alterada" : "registrada";
            Atividades.Add(new AtividadeSistema(
                v.DtEdicao ?? v.DtCriacao,
                $"Venda #{v.IdVenda} {acao}",
                $"R$ {v.VlTotal:N2} · {v.Cliente?.NmCliente ?? "Consumidor"}",
                "bi-cart-check-fill", "#10b981",
                "/Cadastro/Venda", new { id = v.IdVenda }, $"Venda #{v.IdVenda}"));
        }

        foreach (var c in compras)
        {
            var acao = c.TpStatus == "CANCELADA" ? "cancelada"
                : c.TpStatus == "FINALIZADA" ? "finalizada"
                : c.DtEdicao != null ? "alterada" : "lançada";
            Atividades.Add(new AtividadeSistema(
                c.DtEdicao ?? c.DtCriacao,
                $"Compra Nº {c.NrNota} {acao}",
                $"R$ {c.VlTotalCompra:N2} · {c.Fornecedor?.DsRazaoSocial ?? "-"}",
                "bi-cart-plus-fill", "#3b82f6",
                "/Cadastro/Compra",
                new { nrNota = c.NrNota, nrSerie = c.NrSerie, nrModelo = c.NrModelo, idFornecedor = c.IdFornecedor },
                $"Compra Nº {c.NrNota}"));
        }

        foreach (var a in agendamentos)
        {
            var acao = a.TpStatus == "CANCELADO" ? "cancelado"
                : a.TpStatus == "CONCLUIDO" ? "concluído"
                : a.DtEdicao != null ? "alterado" : "marcado";
            Atividades.Add(new AtividadeSistema(
                a.DtEdicao ?? a.DtCriacao,
                $"Agendamento {acao}",
                $"{a.Cliente?.NmCliente ?? "-"} · {a.Servico?.DsServico ?? "-"} · {a.DtAgendamento:dd/MM} {a.HrInicio:HH:mm}",
                "bi-calendar-check-fill", "#f59e0b",
                "/Cadastro/Agendamentos", new { DataFiltro = a.DtAgendamento.ToString("yyyy-MM-dd") }, "Agendamentos"));
        }

        Atividades = Atividades.OrderByDescending(x => x.Data).Take(qtd).ToList();
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
