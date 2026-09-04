using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppERP.Models;

// Mapeia a tabela tbEstCategorias (criada no banco via Database/NormalizarBase.sql).
// Substitui o antigo campo de texto livre tbEstProdutos.dsCategoria.
[Table("tbEstCategorias")]
public class EstCategoria
{
    [Key]
    [Column("idCategoria")]
    public int IdCategoria { get; set; }

    [Column("nmCategoria")]
    [Required(ErrorMessage = "O nome da categoria é obrigatório.")]
    [MaxLength(100)]
    public string NmCategoria { get; set; } = string.Empty;

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
