using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppERP.Models.Gerais;

// Categoria do servico (Corte, Barba...), separada das categorias de produto
// (tbEstCategorias). Tabela criada em
// Database/Migracoes/1794064200000_Create_Table_tbEstUnidadesMedida_tbGerCategoriasServico.sql.
// Cadastro: texto gravado em MAIUSCULO (CaixaAltaAttribute).
[CaixaAlta]
[Table("tbGerCategoriasServico")]
public class CategoriaServico : EntidadeComRegistro
{
    [Key]
    [Column("idCategoriaServico")]
    public int IdCategoriaServico { get; set; }

    [Column("nmCategoria")]
    [Required, MaxLength(100)]
    public string NmCategoria { get; set; } = string.Empty;

    [Column("flAtivo")]
    public bool FlAtivo { get; set; } = true;
}
