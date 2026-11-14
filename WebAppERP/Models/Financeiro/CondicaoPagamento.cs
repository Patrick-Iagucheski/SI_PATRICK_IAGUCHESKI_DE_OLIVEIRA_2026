using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppERP.Models.Financeiro;

// Condicao de pagamento: cabecalho com juros/multa/desconto e parcelas (1:N).
// Cadastro: texto gravado em MAIUSCULO (CaixaAltaAttribute).
[CaixaAlta]
[Table("tbFinCondicoesPagamento")]
public class CondicaoPagamento : EntidadeComRegistro
{
    [Key]
    [Column("idCondicaoPagamento")]
    public int IdCondicaoPagamento { get; set; }

    [Nome]
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

    public List<CondicaoPagamentoParcela> Parcelas { get; set; } = [];
}
