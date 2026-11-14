using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppERP.Models.Financeiro;

// Conta a receber: titulo lancado na tela ou gerado ao finalizar uma venda.
// Cadastro: texto gravado em MAIUSCULO (CaixaAltaAttribute).
[CaixaAlta]
[Table("tbFinContasAReceber")]
public class ContaAReceber : EntidadeComRegistro
{
    // Situacoes do titulo (DsStatus). VENCIDA nao e gravada: e calculada na tela
    // para titulo pendente com vencimento ja passado.
    public const string PENDENTE  = "PENDENTE";
    public const string RECEBIDO  = "RECEBIDO";
    public const string CANCELADO = "CANCELADO";
    public const string VENCIDA   = "VENCIDA";

    [Key]
    [Column("idContaReceber")]
    public int IdContaReceber { get; set; }

    [Column("dsConta")]
    [Required, MaxLength(100)]
    public string DsConta { get; set; } = string.Empty;

    [Column("nrDocumento")]
    [MaxLength(20)]
    public string? NrDocumento { get; set; }

    // Opcional so na venda a vista de consumidor sem cadastro.
    [Column("idCliente")]
    public int? IdCliente { get; set; }

    [ForeignKey("IdCliente")]
    public Cliente? Cliente { get; set; }

    [Column("idFormaPagamento")]
    public int? IdFormaPagamento { get; set; }

    [ForeignKey("IdFormaPagamento")]
    public FormaPagamento? FormaPagamento { get; set; }

    [Column("dtEmissao")]
    public DateOnly? DtEmissao { get; set; }

    [Column("dtVencimento")]
    public DateOnly? DtVencimento { get; set; }

    [Column("dtRecebimento")]
    public DateOnly? DtRecebimento { get; set; }

    [Column("vlValor", TypeName = "decimal(12,2)")]
    public decimal VlValor { get; set; }

    [Column("vlRecebido", TypeName = "decimal(12,2)")]
    public decimal VlRecebido { get; set; }

    // PENDENTE | RECEBIDO | CANCELADO
    [Column("dsStatus")]
    [Required, MaxLength(20)]
    public string DsStatus { get; set; } = PENDENTE;

    [MultiLinha]
    [Column("dsObservacao")]
    [MaxLength(500)]
    public string? DsObservacao { get; set; }

    // ===== Origem: venda que gerou o titulo =====
    // Titulo lancado na tela de Contas a Receber mantem os dois NULL.

    [Column("idVenda")]
    public int? IdVenda { get; set; }

    [Column("nrParcela")]
    public int? NrParcela { get; set; }

    // True quando o titulo foi gerado por uma venda.
    [NotMapped]
    public bool OriginadoDeVenda => IdVenda != null;

    // Parcela a vista da venda: ja nasce recebida, no ato da venda.
    [NotMapped]
    public bool RecebidoNaVenda => OriginadoDeVenda && DtVencimento == DtEmissao;
}
