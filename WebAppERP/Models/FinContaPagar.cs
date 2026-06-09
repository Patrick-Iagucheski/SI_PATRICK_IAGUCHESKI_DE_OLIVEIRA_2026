using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebAppERP.Models;

// Chave primaria composta (igual ao DDL).
// FK para tbFisNFe nao mapeada: o modulo Fiscal de NF-e ainda nao foi criado no projeto.
[PrimaryKey(nameof(NrNFe), nameof(NrSerie), nameof(NrModelo), nameof(IdFornecedor), nameof(NrParcela))]
[Table("tbFinContasAPagar")]
public class FinContaPagar
{
    [Column("nrNFe")]
    public int NrNFe { get; set; }

    [Column("nrSerie")]
    public int NrSerie { get; set; }

    [Column("nrModelo")]
    public int NrModelo { get; set; }

    [Column("idFornecedor")]
    public int IdFornecedor { get; set; }

    [ForeignKey("IdFornecedor")]
    public GerFornecedor? Fornecedor { get; set; }

    [Column("nrParcela")]
    public int NrParcela { get; set; }

    [Column("vlParcela", TypeName = "decimal(12,2)")]
    public decimal? VlParcela { get; set; }

    [Column("dtVencimento")]
    public DateOnly? DtVencimento { get; set; }

    [Column("dtPagamento")]
    public DateOnly? DtPagamento { get; set; }

    [Column("idFormaPagamento")]
    public int? IdFormaPagamento { get; set; }

    [ForeignKey("IdFormaPagamento")]
    public FinFormaPagamento? FormaPagamento { get; set; }

    [Column("dsObservacao")]
    [MaxLength(500)]
    public string? DsObservacao { get; set; }
}
