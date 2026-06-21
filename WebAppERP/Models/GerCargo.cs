using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppERP.Models;

// Mapeia a tabela tbGerCargos (ja criada no banco via SQL).
[Table("tbGerCargos")]
public class GerCargo
{
    [Key]
    [Column("idCargo")]
    public int IdCargo { get; set; }

    [Column("nmCargo")]
    [Required(ErrorMessage = "O nome do cargo e obrigatorio.")]
    [MaxLength(100)]
    public string NmCargo { get; set; } = string.Empty;

    [Column("dsCargo")]
    [MaxLength(300)]
    public string? DsCargo { get; set; }

    [Column("vlComissaoPadrao", TypeName = "decimal(5,2)")]
    [Range(0, 999.99, ErrorMessage = "Comissao invalida.")]
    public decimal? VlComissaoPadrao { get; set; }

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
