using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppERP.Models.Vendas;

[Table("tbVenVendas")]
public class Venda
{
    // Situacoes da venda (TpStatus)
    public const string ABERTA     = "ABERTA";
    public const string FINALIZADA = "FINALIZADA";
    public const string CANCELADA  = "CANCELADA";

    [Key]
    [Column("idVenda")]
    public int IdVenda { get; set; }

    [Column("idCliente")]
    public int? IdCliente { get; set; }

    [ForeignKey("IdCliente")]
    public Cliente? Cliente { get; set; }

    [Column("idFuncionario")]
    public int IdFuncionario { get; set; }

    [ForeignKey("IdFuncionario")]
    public Funcionario? Funcionario { get; set; }

    [Column("idAgendamento")]
    public int? IdAgendamento { get; set; }

    [Column("dtVenda")]
    public DateTime DtVenda { get; set; } = DateTime.Now;

    [Column("vlSubtotal")]
    public decimal VlSubtotal { get; set; }

    [Column("vlDesconto")]
    public decimal VlDesconto { get; set; }

    [Column("vlTotal")]
    public decimal VlTotal { get; set; }

    [Column("idFormaPagamento")]
    public int? IdFormaPagamento { get; set; }

    [Column("tpStatus")]
    [Required, MaxLength(20)]
    public string TpStatus { get; set; } = ABERTA;

    [Column("dsObservacao")]
    [MaxLength(500)]
    public string? DsObservacao { get; set; }

    [Column("dtCriacao")]
    public DateTime DtCriacao { get; set; } = DateTime.Now;

    [Column("idUsuarioEdicao")]
    public int? IdUsuarioEdicao { get; set; }

    [Column("dtEdicao")]
    public DateTime? DtEdicao { get; set; }

    [ForeignKey("IdFormaPagamento")]
    public FormaPagamento? FormaPagamento { get; set; }

    public List<VendaItem> Itens { get; set; } = [];
}
