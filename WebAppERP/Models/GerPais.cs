using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppERP.Models;

[Table("tbGerPaises")]
public class GerPais
{
    [Key]
    [Column("idPais")]
    public int IdPais { get; set; }

    [Column("nmPais")]
    [Required, MaxLength(100)]
    public string NmPais { get; set; } = string.Empty;

    [Column("sgPais")]
    [Required, MaxLength(5)]
    public string SgPais { get; set; } = string.Empty;

    [Column("nrDDI")]
    [MaxLength(5)]
    public string? NrDDI { get; set; }

    [Column("dsMoeda")]
    [MaxLength(50)]
    public string? DsMoeda { get; set; }
}
