namespace WebAppERP.DTOs.Estoque;

// Lancamento manual de estoque (tela de Movimentacao), sem passar pela compra
// ou pela venda.
//   ENTRADA: ajuste que soma ao saldo (ex.: contagem acima do sistema)
//   SAIDA:   ajuste que tira do saldo (ex.: consumo interno, contagem abaixo)
//   PERDA:   saida por quebra, vencimento ou extravio (coluna Perdas do Balancete)
public class LancamentoEstoqueDTO
{
    public const string ENTRADA = "ENTRADA";
    public const string SAIDA   = "SAIDA";
    public const string PERDA   = "PERDA";

    public int IdProduto { get; set; }
    public string TpLancamento { get; set; } = ENTRADA;
    public decimal QtMovimento { get; set; }

    // So na entrada: entra no custo medio do produto.
    public decimal? VlCustoUnitario { get; set; }
    public string? DsObservacao { get; set; }
}
