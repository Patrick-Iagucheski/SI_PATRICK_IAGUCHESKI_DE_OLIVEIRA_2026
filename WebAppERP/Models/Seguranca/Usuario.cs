using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppERP.Models.Seguranca;

[Table("tbSegUsuarios")]
public class Usuario
{
    [Key]
    [Column("idUsuario")]
    public int IdUsuario { get; set; }

    [Nome]
    [Column("nmUsuario")]
    [Required, MaxLength(60)]
    public string NmUsuario { get; set; } = string.Empty;

    [Column("dsLogin")]
    [Required, MaxLength(50)]
    public string DsLogin { get; set; } = string.Empty;

    [TextoBruto]
    [Column("dsSenha")]
    [Required, MaxLength(255)]
    public string DsSenha { get; set; } = string.Empty;

    [Column("flAtivo")]
    public bool FlAtivo { get; set; } = true;

    [Column("dtCriacao")]
    public DateTime DtCriacao { get; set; } = DateTime.Now;
}
