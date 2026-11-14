namespace WebAppERP.DTOs.Estoque;

// Linha do Balancete de Estoque: o que aconteceu com o produto no periodo.
//   Saldo final = inicial + compras - vendas + ajustes - perdas
// Compras e vendas ja descontam os estornos (cancelamentos e alteracoes);
// ajustes podem ser positivos (entrada manual) ou negativos (saida manual).
public class BalanceteEstoqueDTO
{
    // Filtro de saldo final
    public const string SALDO_NEGATIVO = "NEGATIVO";
    public const string SALDO_ZERADO   = "ZERADO";
    public const string SALDO_POSITIVO = "POSITIVO";

    public int IdProduto { get; set; }
    public string DsProduto { get; set; } = string.Empty;
    public string? SgUnidade { get; set; }
    public bool FlAtivo { get; set; }

    public decimal QtSaldoInicial { get; set; }
    public decimal QtCompras { get; set; }
    public decimal QtVendas { get; set; }
    public decimal QtAjustes { get; set; }
    public decimal QtPerdas { get; set; }
    public int QtMovimentos { get; set; }

    public decimal QtSaldoFinal => QtSaldoInicial + QtCompras - QtVendas + QtAjustes - QtPerdas;

    // Custo medio no fim do periodo (o do ultimo movimento ate la; sem movimento,
    // o atual do produto); saldo negativo nao tem valor em estoque.
    public decimal VlCustoMedio { get; set; }
    public decimal VlEstoque => QtSaldoFinal > 0 ? Math.Round(QtSaldoFinal * VlCustoMedio, 2) : 0;
}
