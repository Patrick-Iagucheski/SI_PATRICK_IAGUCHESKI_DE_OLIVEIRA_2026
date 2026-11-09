namespace WebAppERP.DTOs.Principal;

// Dados do painel da tela Principal (indicadores, listas e graficos).
public class PainelDTO
{
    // Indicadores
    public int TotalAgendamentosHoje { get; set; }
    public decimal FaturamentoMes { get; set; }
    public int TotalClientesAtivos { get; set; }
    public int TotalServicosMes { get; set; }

    public List<Agendamento> ProximosAgendamentos { get; set; } = [];
    public List<Venda> UltimasVendas { get; set; } = [];

    // Grafico: faturamento dos ultimos 7 dias
    public List<string> FaturamentoSemanaLabels { get; set; } = [];
    public List<decimal> FaturamentoSemanaValores { get; set; } = [];

    // Painel "Atividades no sistema": ultimas vendas, compras e agendamentos
    // criados ou alterados, do mais recente para o mais antigo
    public List<AtividadeDTO> Atividades { get; set; } = [];
}

// Uma linha do painel de atividades; Pagina + Rota abrem o registro numa aba.
public record AtividadeDTO(
    DateTime Data, string Titulo, string Detalhe, string Icone, string Cor,
    string Pagina, object? Rota, string TituloAba);
