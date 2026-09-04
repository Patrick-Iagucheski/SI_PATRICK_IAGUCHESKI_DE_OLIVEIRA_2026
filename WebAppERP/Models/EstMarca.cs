using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppERP.Models;

// Mapeia a tabela tbEstMarcas (criada no banco via Database/CriarMarcas.sql).
[Table("tbEstMarcas")]
public class EstMarca
{
    [Key]
    [Column("idMarca")]
    public int IdMarca { get; set; }

    [Column("nmMarca")]
    [Required(ErrorMessage = "O nome da marca é obrigatório.")]
    [MaxLength(100)]
    public string NmMarca { get; set; } = string.Empty;

    [Column("flAtivo")]
    public bool FlAtivo { get; set; } = true;

    [Column("idUsuario")]
    public int? IdUsuario { get; set; }

    [Column("dtCriacao")]
    public DateTime DtCriacao { get; set; } = DateTime.Now;

    [Column("idUsuarioEdicao")]
    public int? IdUsuarioEdicao { get; set; }

    [Column("dtEdicao")]
    public DateTime? DtEdicao { get; set; }
}
