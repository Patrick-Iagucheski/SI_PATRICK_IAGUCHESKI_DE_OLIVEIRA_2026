using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebAppERP.Models;

// Item da compra. Chave composta: a chave COMPLETA da compra (4 colunas)
// mais a sequencia do item. O relacionamento com o cabecalho e configurado
// por Fluent API em AppDbContext (FK composta).
[PrimaryKey(nameof(NrNota), nameof(NrSerie), nameof(NrModelo), nameof(IdFornecedor), nameof(NrItem))]
[Table("tbOpeCompraItens")]
public class OpeCompraItem
{
    [Column("nrNota")]
    public int NrNota { get; set; }

    [Column("nrSerie")]
    public int NrSerie { get; set; }

    [Column("nrModelo")]
    public int NrModelo { get; set; }

    [Column("idFornecedor")]
    public int IdFornecedor { get; set; }

    // Sequencia do item dentro da compra (1, 2, 3...).
    [Column("nrItem")]
    public int NrItem { get; set; }

    public OpeCompra? Compra { get; set; }

    // PRODUTO | SERVICO
    [Column("tpItem")]
    [Required, MaxLength(10)]
    public string TpItem { get; set; } = "PRODUTO";

    [Column("idProduto")]
    public int? IdProduto { get; set; }

    [ForeignKey("IdProduto")]
    public EstProduto? Produto { get; set; }

    [Column("idServico")]
    public int? IdServico { get; set; }

    [ForeignKey("IdServico")]
    public GerServico? Servico { get; set; }

    [Column("idClassificacaoConta")]
    public int? IdClassificacaoConta { get; set; }

    [ForeignKey("IdClassificacaoConta")]
    public FinClassificacaoConta? ClassificacaoConta { get; set; }

    // Unidade copiada do produto no momento da compra.
    [Column("sgUnidade")]
    [MaxLength(10)]
    public string? SgUnidade { get; set; }

    [Column("qtItem", TypeName = "decimal(12,4)")]
    public decimal QtItem { get; set; } = 1;

    [Column("vlUnitario", TypeName = "decimal(12,4)")]
    public decimal VlUnitario { get; set; }

    [Column("vlDesconto", TypeName = "decimal(12,2)")]
    public decimal VlDesconto { get; set; }

    [Column("vlTotal", TypeName = "decimal(12,2)")]
    public decimal VlTotal { get; set; }
}
