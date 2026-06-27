using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppERP.Models;

// Conta a pagar financeira (modelo autonomo, chave surrogate idContaPagar).
[Table("tbFinContasAPagar")]
public class FinContaPagar
{
    [Key]
    [Column("idContaPagar")]
    public int IdContaPagar { get; set; }

    [Column("dsConta")]
    [Required, MaxLength(100)]
    public string DsConta { get; set; } = string.Empty;

    [Column("nrDocumento")]
    [MaxLength(20)]
    public string? NrDocumento { get; set; }

    [Column("idFornecedor")]
    public int IdFornecedor { get; set; }

    [ForeignKey("IdFornecedor")]
    public GerFornecedor? Fornecedor { get; set; }

    [Column("idFormaPagamento")]
    public int? IdFormaPagamento { get; set; }

    [ForeignKey("IdFormaPagamento")]
    public FinFormaPagamento? FormaPagamento { get; set; }

    [Column("dtEmissao")]
    public DateOnly? DtEmissao { get; set; }

    [Column("dtVencimento")]
    public DateOnly? DtVencimento { get; set; }

    [Column("dtPagamento")]
    public DateOnly? DtPagamento { get; set; }

    [Column("vlValor", TypeName = "decimal(12,2)")]
    public decimal VlValor { get; set; }

    [Column("vlPago", TypeName = "decimal(12,2)")]
    public decimal VlPago { get; set; }

    // PENDENTE | PAGO | CANCELADO
    [Column("dsStatus")]
    [Required, MaxLength(20)]
    public string DsStatus { get; set; } = "PENDENTE";

    [Column("flAtivo")]
    public bool FlAtivo { get; set; } = true;

    [Column("dsObservacao")]
    [MaxLength(500)]
    public string? DsObservacao { get; set; }

    [Column("dtCriacao")]
    public DateTime DtCriacao { get; set; } = DateTime.Now;

    [Column("dtEdicao")]
    public DateTime? DtEdicao { get; set; }
}
