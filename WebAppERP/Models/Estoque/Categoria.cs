using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppERP.Models.Estoque;

// Mapeia a tabela tbEstCategorias (criada no banco via Database/NormalizarBase.sql).
// Substitui o antigo campo de texto livre tbEstProdutos.dsCategoria.
[Table("tbEstCategorias")]
public class Categoria : EntidadeComRegistro
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
}
