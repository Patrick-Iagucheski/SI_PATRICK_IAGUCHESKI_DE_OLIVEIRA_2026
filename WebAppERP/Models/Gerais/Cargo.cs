using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppERP.Models.Gerais;

// Mapeia a tabela tbGerCargos (ja criada no banco via SQL).
// Cadastro: texto gravado em MAIUSCULO (CaixaAltaAttribute).
[CaixaAlta]
[Table("tbGerCargos")]
public class Cargo : EntidadeComRegistro
{
    [Key]
    [Column("idCargo")]
    public int IdCargo { get; set; }

    [Nome]
    [Column("nmCargo")]
    [Required(ErrorMessage = "O nome do cargo é obrigatório.")]
    [MaxLength(100)]
    public string NmCargo { get; set; } = string.Empty;

    [MultiLinha]
    [Column("dsCargo")]
    [MaxLength(300)]
    public string? DsCargo { get; set; }

    [Column("vlComissaoPadrao", TypeName = "decimal(5,2)")]
    [Range(0, 999.99, ErrorMessage = "Comissão inválida.")]
    public decimal? VlComissaoPadrao { get; set; }

    [Column("flAtivo")]
    public bool FlAtivo { get; set; } = true;
}
