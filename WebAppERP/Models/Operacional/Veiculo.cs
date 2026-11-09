using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppERP.Models.Operacional;

// Cadastro: texto gravado em MAIUSCULO (CaixaAltaAttribute).
[CaixaAlta]
[Table("tbOpeVeiculos")]
public class Veiculo : EntidadeComRegistro
{
    [Key]
    [Column("idVeiculo")]
    public int IdVeiculo { get; set; }

    [Column("dsPlaca")]
    [Required, MaxLength(10)]
    public string DsPlaca { get; set; } = string.Empty;

    [Column("idEstado")]
    public int? IdEstado { get; set; }

    [ForeignKey("IdEstado")]
    public Estado? Estado { get; set; }

    [Column("nrANTT")]
    [MaxLength(20)]
    public string? NrANTT { get; set; }
}
