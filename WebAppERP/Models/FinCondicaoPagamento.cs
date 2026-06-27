using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppERP.Models;

// Condicao de pagamento: cabecalho com juros/multa/desconto e parcelas (1:N).
[Table("tbFinCondicoesPagamento")]
public class FinCondicaoPagamento
{
    [Key]
    [Column("idCondicaoPagamento")]
    public int IdCondicaoPagamento { get; set; }

    [Column("dsCondicao")]
    [Required, MaxLength(100)]
    public string DsCondicao { get; set; } = string.Empty;

    [Column("vlJuro", TypeName = "decimal(7,2)")]
    public decimal VlJuro { get; set; }

    [Column("vlMulta", TypeName = "decimal(7,2)")]
    public decimal VlMulta { get; set; }

    [Column("vlDesconto", TypeName = "decimal(7,2)")]
    public decimal VlDesconto { get; set; }

    [Column("flAtivo")]
    public bool FlAtivo { get; set; } = true;

    [Column("dtCriacao")]
    public DateTime DtCriacao { get; set; } = DateTime.Now;

    [Column("dtEdicao")]
    public DateTime? DtEdicao { get; set; }

    public List<FinCondicaoPagamentoParcela> Parcelas { get; set; } = [];
}
