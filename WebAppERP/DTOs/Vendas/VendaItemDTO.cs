namespace WebAppERP.DTOs.Vendas;

// Item enviado pela tela (totais sao recalculados no servidor por seguranca).
public class VendaItemDTO
{
    public string TpItem { get; set; } = "SERVICO"; // SERVICO | PRODUTO
    public int? IdServico { get; set; }
    public int? IdProduto { get; set; }
    public decimal QtItem { get; set; } = 1;
    public decimal VlUnitario { get; set; }
    public decimal VlDesconto { get; set; }
}
