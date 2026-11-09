using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppERP.Models.Estoque;

// Mapeia a tabela tbEstMarcas (criada no banco via Database/CriarMarcas.sql).
// Cadastro: texto gravado em MAIUSCULO (CaixaAltaAttribute).
[CaixaAlta]
[Table("tbEstMarcas")]
public class Marca : EntidadeComRegistro
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
}
