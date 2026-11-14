using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppERP.Models.Gerais;

// Cadastro: texto gravado em MAIUSCULO (CaixaAltaAttribute).
[CaixaAlta]
[Table("tbGerCidades")]
public class Cidade : EntidadeComRegistro
{
    [Key]
    [Column("idCidade")]
    public int IdCidade { get; set; }

    [Nome]
    [Column("nmCidade")]
    [Required, MaxLength(60)]
    public string NmCidade { get; set; } = string.Empty;

    [Column("idEstado")]
    public int IdEstado { get; set; }

    [ForeignKey("IdEstado")]
    public Estado? Estado { get; set; }
}
