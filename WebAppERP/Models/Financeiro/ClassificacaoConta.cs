using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppERP.Models.Financeiro;

// Classificacao contabil/gerencial usada nos itens da compra
// (ex: Mercadorias para Revenda, Material de Uso e Consumo).
// Tabela criada em Database/CriarCompras.sql.
// Cadastro: texto gravado em MAIUSCULO (CaixaAltaAttribute).
[CaixaAlta]
[Table("tbFinClassificacoesConta")]
public class ClassificacaoConta : EntidadeComRegistro
{
    [Key]
    [Column("idClassificacaoConta")]
    public int IdClassificacaoConta { get; set; }

    [Nome]
    [Column("dsClassificacaoConta")]
    [Required(ErrorMessage = "A descrição da classificação é obrigatória.")]
    [MaxLength(100)]
    public string DsClassificacaoConta { get; set; } = string.Empty;

    [Column("flAtivo")]
    public bool FlAtivo { get; set; } = true;
}
