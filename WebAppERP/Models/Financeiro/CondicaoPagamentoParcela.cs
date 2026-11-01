using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebAppERP.Models.Financeiro;

// Parcela de uma condicao de pagamento. Chave composta (Condicao + Parcela).
[PrimaryKey(nameof(IdCondicaoPagamento), nameof(NrParcela))]
[Table("tbFinCondicoesPagamentoParcelas")]
public class CondicaoPagamentoParcela
{
    [Column("idCondicaoPagamento")]
    public int IdCondicaoPagamento { get; set; }

    [ForeignKey("IdCondicaoPagamento")]
    public CondicaoPagamento? Condicao { get; set; }

    [Column("nrParcela")]
    public int NrParcela { get; set; }

    [Column("nrDiasVencimento")]
    public int NrDiasVencimento { get; set; }

    [Column("idFormaPagamento")]
    public int IdFormaPagamento { get; set; }

    [ForeignKey("IdFormaPagamento")]
    public FormaPagamento? FormaPagamento { get; set; }

    [Column("vlPercentual", TypeName = "decimal(7,2)")]
    public decimal VlPercentual { get; set; }
}
