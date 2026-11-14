using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppERP.Models.Estoque;

// Movimentacao de estoque: cada entrada ou saida de produto, com o saldo que
// ficou depois dela. Gravada so pela compra (entrada) e pela venda (saida),
// em MovimentacaoEstoqueBusiness.Registrar. Texto gravado em MAIUSCULO.
[CaixaAlta]
[Table("tbEstMovimentacoes")]
public class MovimentacaoEstoque : EntidadeComRegistro
{
    // Tipos (TpMovimento)
    public const string ENTRADA = "ENTRADA";
    public const string SAIDA   = "SAIDA";

    // Origens (DsOrigem)
    public const string COMPRA = "COMPRA";
    public const string VENDA  = "VENDA";

    [Key]
    [Column("idMovimentacao")]
    public int IdMovimentacao { get; set; }

    [Column("idProduto")]
    public int IdProduto { get; set; }

    [ForeignKey("IdProduto")]
    public Produto? Produto { get; set; }

    [Column("dtMovimento")]
    public DateTime DtMovimento { get; set; }

    // ENTRADA | SAIDA
    [Column("tpMovimento")]
    [Required, MaxLength(10)]
    public string TpMovimento { get; set; } = ENTRADA;

    // COMPRA | VENDA
    [Column("dsOrigem")]
    [Required, MaxLength(20)]
    public string DsOrigem { get; set; } = COMPRA;

    // Sempre positiva; o sentido vem do TpMovimento.
    [Column("qtMovimento", TypeName = "decimal(12,4)")]
    public decimal QtMovimento { get; set; }

    // Saldo do produto depois deste movimento.
    [Column("qtSaldo", TypeName = "decimal(12,4)")]
    public decimal QtSaldo { get; set; }

    [Column("vlCustoUnitario", TypeName = "decimal(18,6)")]
    public decimal? VlCustoUnitario { get; set; }

    // Custo medio do produto depois deste movimento (Balancete valoriza o
    // estoque de um periodo pelo custo daquela epoca).
    [Column("vlCustoMedio", TypeName = "decimal(18,6)")]
    public decimal? VlCustoMedio { get; set; }

    // Desfaz uma compra ou venda (cancelamento ou alteracao).
    [Column("flEstorno")]
    public bool FlEstorno { get; set; }

    // ===== Origem: venda ou compra (chave completa) =====

    [Column("idVenda")]
    public int? IdVenda { get; set; }

    [ForeignKey("IdVenda")]
    public Venda? Venda { get; set; }

    [Column("nrNota")]
    [MaxLength(9)]
    public string? NrNota { get; set; }

    [Column("nrSerie")]
    [MaxLength(3)]
    public string? NrSerie { get; set; }

    [Column("nrModelo")]
    public int? NrModelo { get; set; }

    [Column("idFornecedor")]
    public int? IdFornecedor { get; set; }

    // Ligacao configurada no AppDbContext (chave composta da compra).
    public Compra? Compra { get; set; }

    [MultiLinha]
    [Column("dsObservacao")]
    [MaxLength(500)]
    public string? DsObservacao { get; set; }

    // Quantidade com sinal: positiva na entrada, negativa na saida.
    [NotMapped]
    public decimal QtComSinal => TpMovimento == ENTRADA ? QtMovimento : -QtMovimento;
}
