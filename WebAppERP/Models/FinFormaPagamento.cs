using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppERP.Models;

[Table("tbFinFormasPagamento")]
public class FinFormaPagamento
{
    [Key]
    [Column("idFormaPagamento")]
    public int IdFormaPagamento { get; set; }

    [Column("dsFormaPagamento")]
    [Required, MaxLength(100)]
    public string DsFormaPagamento { get; set; } = string.Empty;

    [Column("flAtivo")]
    public bool FlAtivo { get; set; } = true;

    [Column("dtCriacao")]
    public DateTime DtCriacao { get; set; } = DateTime.Now;
}
