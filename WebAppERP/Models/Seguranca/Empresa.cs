using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppERP.Models.Seguranca;

// Mapeia a tabela tbSegEmpresas (criada em Database/CriarEmpresa.sql como
// tbGerEmpresa e renomeada pela migration em Database/Migracoes).
// Uma linha so: nome e logo da empresa exibidos no menu lateral.
[Table("tbSegEmpresas")]
public class Empresa : EntidadeComRegistro
{
    [Key]
    [Column("idEmpresa")]
    public int IdEmpresa { get; set; }

    [Nome]
    [Column("nmEmpresa")]
    [Required(ErrorMessage = "O nome da empresa é obrigatório.")]
    [MaxLength(100)]
    public string NmEmpresa { get; set; } = string.Empty;

    // Caminho relativo da logo salva em wwwroot (ex: /uploads/empresa/xxx.png).
    // Nulo = usa a logo padrao (images/logo.png).
    [Column("dsLogo")]
    [MaxLength(300)]
    public string? DsLogo { get; set; }
}
