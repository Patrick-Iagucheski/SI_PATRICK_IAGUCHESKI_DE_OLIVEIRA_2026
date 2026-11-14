using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppERP.Models.Estoque;

// Unidade de medida do produto (UN - Unidade, KG - Quilograma...). Tabela
// criada em Database/Migracoes/1794064200000_Create_Table_tbEstUnidadesMedida_tbGerCategoriasServico.sql.
// Cadastro: texto gravado em MAIUSCULO (CaixaAltaAttribute).
[CaixaAlta]
[Table("tbEstUnidadesMedida")]
public class UnidadeMedida : EntidadeComRegistro
{
    [Key]
    [Column("idUnidadeMedida")]
    public int IdUnidadeMedida { get; set; }

    [Column("sgUnidade")]
    [Required, MaxLength(10)]
    public string SgUnidade { get; set; } = string.Empty;

    [Nome]
    [Column("dsUnidade")]
    [Required, MaxLength(60)]
    public string DsUnidade { get; set; } = string.Empty;

    [Column("flAtivo")]
    public bool FlAtivo { get; set; } = true;

    // Texto das listas e do campo do produto (ex.: "UN - Unidade").
    [NotMapped]
    public string Descricao => $"{SgUnidade} - {DsUnidade}";
}
