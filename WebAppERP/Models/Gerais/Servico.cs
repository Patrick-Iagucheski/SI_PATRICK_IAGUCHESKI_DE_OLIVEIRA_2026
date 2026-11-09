using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppERP.Models.Gerais;

// Cadastro: texto gravado em MAIUSCULO (CaixaAltaAttribute).
[CaixaAlta]
[Table("tbGerServicos")]
public class Servico : EntidadeComRegistro
{
    [Key]
    [Column("idServico")]
    public int IdServico { get; set; }

    [Column("dsServico")]
    [Required, MaxLength(200)]
    public string DsServico { get; set; } = string.Empty;

    [Column("dsDescricao")]
    [MaxLength(500)]
    public string? DsDescricao { get; set; }

    [Column("vlServico")]
    public decimal VlServico { get; set; }

    [Column("nrDuracaoMin")]
    public int? NrDuracaoMin { get; set; }

    // Categoria: vinculo com o cadastro de Categorias de Servico
    // (tbGerCategoriasServico). Substituiu o antigo texto tbGerServicos.dsCategoria.
    [Column("idCategoriaServico")]
    public int? IdCategoriaServico { get; set; }

    [ForeignKey("IdCategoriaServico")]
    public CategoriaServico? CategoriaServico { get; set; }

    [Column("flAtivo")]
    public bool FlAtivo { get; set; } = true;
}
