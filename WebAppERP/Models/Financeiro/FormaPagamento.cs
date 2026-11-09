using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppERP.Models.Financeiro;

// Cadastro: texto gravado em MAIUSCULO (CaixaAltaAttribute).
[CaixaAlta]
[Table("tbFinFormasPagamento")]
public class FormaPagamento : EntidadeComRegistro
{
    [Key]
    [Column("idFormaPagamento")]
    public int IdFormaPagamento { get; set; }

    [Column("dsFormaPagamento")]
    [Required, MaxLength(100)]
    public string DsFormaPagamento { get; set; } = string.Empty;

    [Column("flAtivo")]
    public bool FlAtivo { get; set; } = true;
}
