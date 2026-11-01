using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebAppERP.Models.Compras;

// Parcela / pagamento da compra. E a base para a geracao dos titulos
// no Contas a Pagar quando a compra e FINALIZADA (1 parcela = 1 titulo).
[PrimaryKey(nameof(NrNota), nameof(NrSerie), nameof(NrModelo), nameof(IdFornecedor), nameof(NrParcela))]
[Table("tbCmpComprasPagamentos")]
public class CompraPagamento
{
    [Column("nrNota")]
    public int NrNota { get; set; }

    [Column("nrSerie")]
    public int NrSerie { get; set; }

    [Column("nrModelo")]
    public int NrModelo { get; set; }

    [Column("idFornecedor")]
    public int IdFornecedor { get; set; }

    // Sequencia da parcela dentro da compra (1, 2, 3...).
    [Column("nrParcela")]
    public int NrParcela { get; set; }

    public Compra? Compra { get; set; }

    [Column("idCondicaoPagamento")]
    public int? IdCondicaoPagamento { get; set; }

    [ForeignKey("IdCondicaoPagamento")]
    public CondicaoPagamento? CondicaoPagamento { get; set; }

    [Column("idFormaPagamento")]
    public int IdFormaPagamento { get; set; }

    [ForeignKey("IdFormaPagamento")]
    public FormaPagamento? FormaPagamento { get; set; }

    [Column("vlParcela", TypeName = "decimal(12,2)")]
    public decimal VlParcela { get; set; }

    [Column("dtVencimento")]
    public DateOnly DtVencimento { get; set; } = DateOnly.FromDateTime(DateTime.Today);
}
