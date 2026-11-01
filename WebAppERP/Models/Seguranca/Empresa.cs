using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppERP.Models.Seguranca;

// Mapeia a tabela tbSegEmpresas (criada em Database/CriarEmpresa.sql como
// tbGerEmpresa e renomeada pela migration em Database/Migracoes).
// Uma linha so: nome e logo da empresa exibidos no menu lateral.
[Table("tbSegEmpresas")]
public class Empresa
{
    [Key]
    [Column("idEmpresa")]
    public int IdEmpresa { get; set; }

    [Column("nmEmpresa")]
    [Required(ErrorMessage = "O nome da empresa é obrigatório.")]
    [MaxLength(100)]
    public string NmEmpresa { get; set; } = string.Empty;

    // Caminho relativo da logo salva em wwwroot (ex: /uploads/empresa/xxx.png).
    // Nulo = usa a logo padrao (images/logo.png).
    [Column("dsLogo")]
    [MaxLength(300)]
    public string? DsLogo { get; set; }

    [Column("idUsuario")]
    public int? IdUsuario { get; set; }

    [Column("dtCriacao")]
    public DateTime DtCriacao { get; set; } = DateTime.Now;

    [Column("idUsuarioEdicao")]
    public int? IdUsuarioEdicao { get; set; }

    [Column("dtEdicao")]
    public DateTime? DtEdicao { get; set; }
}
