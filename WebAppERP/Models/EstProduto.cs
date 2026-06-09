using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppERP.Models;

[Table("tbEstProdutos")]
public class EstProduto
{
    [Key]
    [Column("idProduto")]
    public int IdProduto { get; set; }

    [Column("dsProduto")]
    [Required, MaxLength(300)]
    public string DsProduto { get; set; } = string.Empty;

    // FK para tbFisNCMSH ainda nao implementada no projeto -> mantido como texto.
    [Column("idNCMSH")]
    [MaxLength(10)]
    public string? IdNCMSH { get; set; }

    [Column("sgUnidade")]
    [MaxLength(10)]
    public string? SgUnidade { get; set; }

    [Column("vlPesoBruto", TypeName = "decimal(12,4)")]
    public decimal? VlPesoBruto { get; set; }

    [Column("vlPesoLiquido", TypeName = "decimal(12,4)")]
    public decimal? VlPesoLiquido { get; set; }

    [Column("qtSaldo", TypeName = "decimal(12,4)")]
    public decimal? QtSaldo { get; set; }

    [Column("vlCustoMedio", TypeName = "decimal(12,4)")]
    public decimal? VlCustoMedio { get; set; }
}
