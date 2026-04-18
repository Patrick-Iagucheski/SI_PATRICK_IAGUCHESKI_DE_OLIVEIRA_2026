using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppERP.Models;

[Table("tbOpeAgendamentos")]
public class OpeAgendamento
{
    [Key]
    [Column("idAgendamento")]
    public int IdAgendamento { get; set; }

    [Column("idCliente")]
    public int IdCliente { get; set; }

    [ForeignKey("IdCliente")]
    public GerCliente? Cliente { get; set; }

    [Column("idFuncionario")]
    public int IdFuncionario { get; set; }

    [ForeignKey("IdFuncionario")]
    public GerFuncionario? Funcionario { get; set; }

    [Column("idServico")]
    public int IdServico { get; set; }

    [ForeignKey("IdServico")]
    public GerServico? Servico { get; set; }

    [Column("dtAgendamento")]
    public DateOnly DtAgendamento { get; set; }

    [Column("hrInicio")]
    public TimeOnly HrInicio { get; set; }

    [Column("hrFim")]
    public TimeOnly? HrFim { get; set; }

    [Column("tpStatus")]
    [Required, MaxLength(20)]
    public string TpStatus { get; set; } = "AGENDADO";

    [Column("dsObservacao")]
    [MaxLength(500)]
    public string? DsObservacao { get; set; }

    [Column("dtCriacao")]
    public DateTime DtCriacao { get; set; } = DateTime.Now;

    [Column("idUsuarioEdicao")]
    public int? IdUsuarioEdicao { get; set; }

    [Column("dtEdicao")]
    public DateTime? DtEdicao { get; set; }
}
