using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppERP.Models.Financeiro;

// Conta a pagar financeira (modelo autonomo, chave surrogate idContaPagar).
// Cadastro: texto gravado em MAIUSCULO (CaixaAltaAttribute).
[CaixaAlta]
[Table("tbFinContasAPagar")]
public class ContaAPagar : EntidadeComRegistro
{
    // Situacoes do titulo (DsStatus). VENCIDA nao e gravada: e calculada na tela
    // para titulo pendente com vencimento ja passado.
    public const string PENDENTE  = "PENDENTE";
    public const string PAGO      = "PAGO";
    public const string CANCELADO = "CANCELADO";
    public const string VENCIDA   = "VENCIDA";

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
    public Fornecedor? Fornecedor { get; set; }

    [Column("idFormaPagamento")]
    public int? IdFormaPagamento { get; set; }

    [ForeignKey("IdFormaPagamento")]
    public FormaPagamento? FormaPagamento { get; set; }

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
    public string DsStatus { get; set; } = PENDENTE;

    // Sem flAtivo: o titulo funciona so pela situacao (dsStatus). Cancelado
    // sai da lista padrao da tela
    // (Database/Migracoes/1794069000000_Alter_Table_tbFinContasAPagar_Remove_flAtivo.sql).

    [Column("dsObservacao")]
    [MaxLength(500)]
    public string? DsObservacao { get; set; }

    // ===== Origem: compra que gerou o titulo =====
    // Preenchido apenas quando o titulo nasce da finalizacao de uma compra.
    // Titulo lancado manualmente na tela de Contas a Pagar mantem tudo NULL.
    // Junto com idFornecedor (acima) formam a chave da parcela de origem:
    // nrNota + nrSerie + nrModelo + idFornecedor + nrParcela.

    [Column("nrNota")]
    [MaxLength(9)]
    public string? NrNota { get; set; }

    [Column("nrSerie")]
    [MaxLength(3)]
    public string? NrSerie { get; set; }

    [Column("nrModelo")]
    public int? NrModelo { get; set; }

    [Column("nrParcela")]
    public int? NrParcela { get; set; }

    // True quando o titulo foi originado por uma compra.
    [NotMapped]
    public bool OriginadoDeCompra => NrNota != null;
}
