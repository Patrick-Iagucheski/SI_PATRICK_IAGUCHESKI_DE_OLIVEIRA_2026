using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebAppERP.Models;

// Parcela de uma condicao de pagamento. Chave composta (Condicao + Parcela).
[PrimaryKey(nameof(IdCondicaoPagamento), nameof(NrParcela))]
[Table("tbFinCondicoesPagamentoParcelas")]
public class FinCondicaoPagamentoParcela
{
    [Column("idCondicaoPagamento")]
    public int IdCondicaoPagamento { get; set; }

    [ForeignKey("IdCondicaoPagamento")]
    public FinCondicaoPagamento? Condicao { get; set; }

    [Column("nrParcela")]
    public int NrParcela { get; set; }

    [Column("nrDiasVencimento")]
    public int NrDiasVencimento { get; set; }

    [Column("idFormaPagamento")]
    public int IdFormaPagamento { get; set; }

    [ForeignKey("IdFormaPagamento")]
    public FinFormaPagamento? FormaPagamento { get; set; }

    [Column("vlPercentual", TypeName = "decimal(7,2)")]
    public decimal VlPercentual { get; set; }
}
