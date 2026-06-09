using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppERP.Models;

[Table("tbGerTransportadores")]
public class GerTransportador
{
    [Key]
    [Column("idTransportador")]
    public int IdTransportador { get; set; }

    [Column("dsRazaoSocial")]
    [MaxLength(200)]
    public string? DsRazaoSocial { get; set; }

    [Column("nrCpfCnpj")]
    [MaxLength(18)]
    public string? NrCpfCnpj { get; set; }

    [Column("dsEndereco")]
    [MaxLength(200)]
    public string? DsEndereco { get; set; }

    [Column("idCidade")]
    public int? IdCidade { get; set; }

    [ForeignKey("IdCidade")]
    public GerCidade? Cidade { get; set; }

    [Column("nrInscEstadual")]
    [MaxLength(20)]
    public string? NrInscEstadual { get; set; }
}
