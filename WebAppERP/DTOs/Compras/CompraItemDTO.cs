namespace WebAppERP.DTOs.Compras;

// Item enviado pela tela. Os totais sao recalculados no servidor por seguranca.
public class CompraItemDTO
{
    public string TpItem { get; set; } = "PRODUTO"; // PRODUTO | SERVICO
    public int? IdProduto { get; set; }
    public int? IdServico { get; set; }
    // Saiu da tela (pedido do professor). So vem preenchida em item antigo que
    // ja tinha classificacao, para ela nao se perder ao editar a compra.
    public int? IdClassificacaoConta { get; set; }
    public string? SgUnidade { get; set; }
    public decimal QtItem { get; set; } = 1;
    public decimal VlUnitario { get; set; }
    public decimal VlDesconto { get; set; }
}
