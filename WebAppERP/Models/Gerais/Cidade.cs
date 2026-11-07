using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppERP.Models.Gerais;

[Table("tbGerCidades")]
public class Cidade : EntidadeComRegistro
{
    [Key]
    [Column("idCidade")]
    public int IdCidade { get; set; }

    [Column("nmCidade")]
    [Required, MaxLength(150)]
    public string NmCidade { get; set; } = string.Empty;

    [Column("idEstado")]
    public int IdEstado { get; set; }

    [ForeignKey("IdEstado")]
    public Estado? Estado { get; set; }
}
