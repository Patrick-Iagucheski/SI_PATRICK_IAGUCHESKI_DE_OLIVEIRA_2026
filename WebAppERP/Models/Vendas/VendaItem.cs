using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppERP.Models.Vendas;

[Table("tbVenVendasItens")]
public class VendaItem
{
    // Tipos de item (TpItem)
    public const string PRODUTO = "PRODUTO";
    public const string SERVICO = "SERVICO";

    [Key]
    [Column("idVendaItem")]
    public int IdVendaItem { get; set; }

    [Column("idVenda")]
    public int IdVenda { get; set; }

    [ForeignKey("IdVenda")]
    public Venda? Venda { get; set; }

    [Column("tpItem")]
    [Required, MaxLength(10)]
    public string TpItem { get; set; } = SERVICO;

    [Column("idServico")]
    public int? IdServico { get; set; }

    [ForeignKey("IdServico")]
    public Servico? Servico { get; set; }

    [Column("idProduto")]
    public int? IdProduto { get; set; }

    [ForeignKey("IdProduto")]
    public Produto? Produto { get; set; }

    [Column("qtItem", TypeName = "decimal(12,4)")]
    public decimal QtItem { get; set; } = 1;

    [Column("vlUnitario", TypeName = "decimal(12,4)")]
    public decimal VlUnitario { get; set; }

    [Column("vlDesconto", TypeName = "decimal(12,2)")]
    public decimal VlDesconto { get; set; }

    [Column("vlTotal", TypeName = "decimal(12,2)")]
    public decimal VlTotal { get; set; }
}
