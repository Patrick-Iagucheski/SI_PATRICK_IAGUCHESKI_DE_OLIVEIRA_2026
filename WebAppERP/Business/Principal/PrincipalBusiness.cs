using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;
using WebAppERP.DTOs.Principal;

namespace WebAppERP.Business.Principal;

// Monta o painel da tela Principal: indicadores do dia e do mes, proximos
// agendamentos, ultimas vendas, graficos e atividades recentes. Somente leitura.
public class PrincipalBusiness
{
    private readonly AppDbContext _db;

    public PrincipalBusiness(AppDbContext db) => _db = db;

    // Quantidade de linhas do painel de atividades.
    private const int QtdAtividades = 10;

    public async Task<PainelDTO> GetPainel(DateOnly hoje)
    {
        var inicioMes = new DateOnly(hoje.Year, hoje.Month, 1);
        var painel = new PainelDTO();

        painel.TotalAgendamentosHoje = await _db.Agendamentos
            .CountAsync(a => a.DtAgendamento == hoje && a.TpStatus != Agendamento.CANCELADO);

        painel.FaturamentoMes = await _db.Vendas
            .Where(v => v.DtVenda.Month == hoje.Month && v.DtVenda.Year == hoje.Year && v.TpStatus == Venda.FINALIZADA)
            .SumAsync(v => v.VlTotal);

        painel.TotalClientesAtivos = await _db.Clientes.CountAsync(c => c.FlAtivo);

        painel.TotalServicosMes = await _db.Agendamentos
            .CountAsync(a => a.DtAgendamento >= inicioMes && a.DtAgendamento <= hoje && a.TpStatus == Agendamento.CONCLUIDO);

        painel.ProximosAgendamentos = await _db.Agendamentos
            .Include(a => a.Cliente)
            .Include(a => a.Funcionario)
            .Include(a => a.Servico)
            .Where(a => a.DtAgendamento >= hoje && a.TpStatus != Agendamento.CANCELADO)
            .OrderBy(a => a.DtAgendamento).ThenBy(a => a.HrInicio)
            .Take(5)
            .ToListAsync();

        painel.UltimasVendas = await _db.Vendas
            .Include(v => v.Cliente)
            .OrderByDescending(v => v.DtVenda)
            .Take(5)
            .ToListAsync();

        await CarregarGraficos(painel, hoje, inicioMes);
        painel.Atividades = await GetAtividades();

        return painel;
    }

    private async Task<List<AtividadeDTO>> GetAtividades()
    {
        var atividades = new List<AtividadeDTO>();

        var vendas = await _db.Vendas
            .Include(v => v.Cliente)
            .OrderByDescending(v => v.DtEdicao ?? v.DtCriacao)
            .Take(QtdAtividades)
            .ToListAsync();

        var compras = await _db.Compras
            .Include(c => c.Fornecedor)
            .OrderByDescending(c => c.DtEdicao ?? c.DtCriacao)
            .Take(QtdAtividades)
            .ToListAsync();

        var agendamentos = await _db.Agendamentos
            .Include(a => a.Cliente)
            .Include(a => a.Servico)
            .OrderByDescending(a => a.DtEdicao ?? a.DtCriacao)
            .Take(QtdAtividades)
            .ToListAsync();

        foreach (var v in vendas)
        {
            var acao = v.TpStatus == Venda.CANCELADA ? "cancelada"
                : v.TpStatus == Venda.FINALIZADA ? "finalizada"
                : v.DtEdicao != null ? "alterada" : "registrada";
            atividades.Add(new AtividadeDTO(
                v.DtEdicao ?? v.DtCriacao,
                $"Venda #{v.IdVenda} {acao}",
                $"R$ {v.VlTotal:N2} · {v.Cliente?.NmCliente ?? "Consumidor"}",
                "bi-cart-check-fill", "#10b981",
                "/Vendas/Venda", new { id = v.IdVenda }, $"Venda #{v.IdVenda}"));
        }

        foreach (var c in compras)
        {
            var acao = c.TpStatus == Compra.CANCELADA ? "cancelada"
                : c.TpStatus == Compra.FINALIZADA ? "finalizada"
                : c.DtEdicao != null ? "alterada" : "lançada";
            atividades.Add(new AtividadeDTO(
                c.DtEdicao ?? c.DtCriacao,
                $"Compra Nº {c.NrNota} {acao}",
                $"R$ {c.VlTotalCompra:N2} · {c.Fornecedor?.DsRazaoSocial ?? "-"}",
                "bi-cart-plus-fill", "#3b82f6",
                "/Compras/Compra",
                new { nrNota = c.NrNota, nrSerie = c.NrSerie, nrModelo = c.NrModelo, idFornecedor = c.IdFornecedor },
                $"Compra Nº {c.NrNota}"));
        }

        foreach (var a in agendamentos)
        {
            var acao = a.TpStatus == Agendamento.CANCELADO ? "cancelado"
                : a.TpStatus == Agendamento.CONCLUIDO ? "concluído"
                : a.DtEdicao != null ? "alterado" : "marcado";
            atividades.Add(new AtividadeDTO(
                a.DtEdicao ?? a.DtCriacao,
                $"Agendamento {acao}",
                $"{a.Cliente?.NmCliente ?? "-"} · {a.Servico?.DsServico ?? "-"} · {a.DtAgendamento:dd/MM} {a.HrInicio:HH:mm}",
                "bi-calendar-check-fill", "#f59e0b",
                "/Operacional/Agendamentos", new { DataFiltro = a.DtAgendamento.ToString("yyyy-MM-dd") }, "Agendamentos"));
        }

        return atividades.OrderByDescending(x => x.Data).Take(QtdAtividades).ToList();
    }

    private async Task CarregarGraficos(PainelDTO painel, DateOnly hoje, DateOnly inicioMes)
    {
        // Faturamento dos ultimos 7 dias (vendas finalizadas), agrupado por dia
        var inicioSemana = hoje.AddDays(-6);
        var inicioSemanaDt = inicioSemana.ToDateTime(TimeOnly.MinValue);
        var fimSemanaDt = hoje.ToDateTime(TimeOnly.MaxValue);

        var vendasSemana = await _db.Vendas
            .Where(v => v.TpStatus == Venda.FINALIZADA && v.DtVenda >= inicioSemanaDt && v.DtVenda <= fimSemanaDt)
            .Select(v => new { v.DtVenda, v.VlTotal })
            .ToListAsync();

        var porDia = vendasSemana
            .GroupBy(v => DateOnly.FromDateTime(v.DtVenda))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.VlTotal));

        string[] diasSemana = ["Dom", "Seg", "Ter", "Qua", "Qui", "Sex", "Sab"];
        for (var dia = inicioSemana; dia <= hoje; dia = dia.AddDays(1))
        {
            painel.FaturamentoSemanaLabels.Add(diasSemana[(int)dia.DayOfWeek]);
            painel.FaturamentoSemanaValores.Add(porDia.TryGetValue(dia, out var total) ? total : 0m);
        }

        // Servicos mais solicitados no mes (top 5 por quantidade de agendamentos)
        var servicosTop = await _db.Agendamentos
            .Where(a => a.DtAgendamento >= inicioMes && a.DtAgendamento <= hoje
                        && a.TpStatus != Agendamento.CANCELADO && a.Servico != null)
            .GroupBy(a => a.Servico!.DsServico)
            .Select(g => new { Servico = g.Key, Quantidade = g.Count() })
            .OrderByDescending(g => g.Quantidade)
            .Take(5)
            .ToListAsync();

        foreach (var item in servicosTop)
        {
            painel.ServicosLabels.Add(item.Servico);
            painel.ServicosValores.Add(item.Quantidade);
        }
    }
}
