using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebAppERP.Models;

// Cabecalho da compra (entrada de nota do fornecedor).
// A compra NAO tem chave surrogate: e identificada pela chave composta
// Numero + Serie + Modelo + Fornecedor, garantida pela PK do banco.
[PrimaryKey(nameof(NrNota), nameof(NrSerie), nameof(NrModelo), nameof(IdFornecedor))]
[Table("tbOpeCompras")]
public class OpeCompra
{
    // ===== Chave composta =====

    [Column("nrNota")]
    public int NrNota { get; set; }

    [Column("nrSerie")]
    public int NrSerie { get; set; }

    // Modelo fiscal: 55 = NFe, 65 = NFCe, 1 = NF modelo 1.
    [Column("nrModelo")]
    public int NrModelo { get; set; }

    [Column("idFornecedor")]
    public int IdFornecedor { get; set; }

    [ForeignKey("IdFornecedor")]
    public GerFornecedor? Fornecedor { get; set; }

    // ===== Dados da nota =====

    [Column("dtEmissao")]
    public DateOnly DtEmissao { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Column("dtChegada")]
    public DateOnly? DtChegada { get; set; }

    [Column("idTransportador")]
    public int? IdTransportador { get; set; }

    [ForeignKey("IdTransportador")]
    public GerTransportador? Transportador { get; set; }

    [Column("idCondicaoPagamento")]
    public int? IdCondicaoPagamento { get; set; }

    [ForeignKey("IdCondicaoPagamento")]
    public FinCondicaoPagamento? CondicaoPagamento { get; set; }

    // ===== Custos e totais (sempre recalculados no servidor) =====

    [Column("vlFrete", TypeName = "decimal(12,2)")]
    public decimal VlFrete { get; set; }

    [Column("vlSeguro", TypeName = "decimal(12,2)")]
    public decimal VlSeguro { get; set; }

    [Column("vlOutrasDespesas", TypeName = "decimal(12,2)")]
    public decimal VlOutrasDespesas { get; set; }

    [Column("vlTotalProdutos", TypeName = "decimal(12,2)")]
    public decimal VlTotalProdutos { get; set; }

    [Column("vlTotalCompra", TypeName = "decimal(12,2)")]
    public decimal VlTotalCompra { get; set; }

    // Custo adicional = frete + seguro + outros gastos (nao persistido).
    [NotMapped]
    public decimal VlCustoAdicional => VlFrete + VlSeguro + VlOutrasDespesas;

    // ===== Situacao =====

    // RASCUNHO | FINALIZADA | CANCELADA
    [Column("tpStatus")]
    [Required, MaxLength(20)]
    public string TpStatus { get; set; } = "RASCUNHO";

    [Column("dsObservacao")]
    [MaxLength(500)]
    public string? DsObservacao { get; set; }

    // ===== Auditoria =====

    [Column("flAtivo")]
    public bool FlAtivo { get; set; } = true;

    [Column("dtCriacao")]
    public DateTime DtCriacao { get; set; } = DateTime.Now;

    [Column("idUsuarioEdicao")]
    public int? IdUsuarioEdicao { get; set; }

    [Column("dtEdicao")]
    public DateTime? DtEdicao { get; set; }

    public List<OpeCompraItem> Itens { get; set; } = [];
    public List<OpeCompraPagamento> Pagamentos { get; set; } = [];
}
