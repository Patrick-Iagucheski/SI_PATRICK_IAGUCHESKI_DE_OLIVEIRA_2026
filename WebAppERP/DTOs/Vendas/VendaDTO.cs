namespace WebAppERP.DTOs.Vendas;

// Payload completo da venda (cabecalho + itens).
public class VendaDTO
{
    public int IdVenda { get; set; }
    public int? IdCliente { get; set; }
    public int IdFuncionario { get; set; }
    public int? IdAgendamento { get; set; }
    public int? IdFormaPagamento { get; set; }
    public int? IdCondicaoPagamento { get; set; }
    public string TpStatus { get; set; } = Venda.ABERTA; // ABERTA | FINALIZADA | CANCELADA
    public decimal VlDesconto { get; set; }          // desconto geral
    public string? DsObservacao { get; set; }
    public List<VendaItemDTO> Itens { get; set; } = [];
}
