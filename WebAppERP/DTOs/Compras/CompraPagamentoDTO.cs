namespace WebAppERP.DTOs.Compras;

// Parcela enviada pela tela.
public class CompraPagamentoDTO
{
    public int? IdCondicaoPagamento { get; set; }
    public int IdFormaPagamento { get; set; }
    public decimal VlParcela { get; set; }
    public DateOnly? DtVencimento { get; set; }
}
