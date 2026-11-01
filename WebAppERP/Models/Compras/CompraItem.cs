using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebAppERP.Models.Compras;

// Item da compra. Chave composta: a chave COMPLETA da compra (4 colunas)
// mais a sequencia do item. O relacionamento com o cabecalho e configurado
// por Fluent API em AppDbContext (FK composta).
[PrimaryKey(nameof(NrNota), nameof(NrSerie), nameof(NrModelo), nameof(IdFornecedor), nameof(NrItem))]
[Table("tbCmpComprasItens")]
public class CompraItem
{
    // Tipos de item (TpItem)
    public const string PRODUTO = "PRODUTO";
    public const string SERVICO = "SERVICO";

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

    public Compra? Compra { get; set; }

    // PRODUTO | SERVICO
    [Column("tpItem")]
    [Required, MaxLength(10)]
    public string TpItem { get; set; } = PRODUTO;

    [Column("idProduto")]
    public int? IdProduto { get; set; }

    [ForeignKey("IdProduto")]
    public Produto? Produto { get; set; }

    [Column("idServico")]
    public int? IdServico { get; set; }

    [ForeignKey("IdServico")]
    public Servico? Servico { get; set; }

    [Column("idClassificacaoConta")]
    public int? IdClassificacaoConta { get; set; }

    [ForeignKey("IdClassificacaoConta")]
    public ClassificacaoConta? ClassificacaoConta { get; set; }

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

    // Parcela do custo adicional (frete + seguro + outros) rateada para
    // este item, proporcional ao seu total. So produtos recebem rateio.
    // Colunas criadas em Database/AlterCompraItensRateio.sql.
    [Column("vlRateio", TypeName = "decimal(12,2)")]
    public decimal VlRateio { get; set; }

    // Custo unitario final = (vlTotal + vlRateio) / qtItem, arredondado em
    // 2 casas (a coluna aceita 4, mas o sistema grava so 2).
    [Column("vlCustoUnitario", TypeName = "decimal(12,4)")]
    public decimal VlCustoUnitario { get; set; }
}
