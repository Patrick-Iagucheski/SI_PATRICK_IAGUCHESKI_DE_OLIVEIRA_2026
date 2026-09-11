using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppERP.Models;

// Classificacao contabil/gerencial usada nos itens da compra
// (ex: Mercadorias para Revenda, Material de Uso e Consumo).
// Tabela criada em Database/CriarCompras.sql.
[Table("tbFinClassificacoesConta")]
public class FinClassificacaoConta
{
    [Key]
    [Column("idClassificacaoConta")]
    public int IdClassificacaoConta { get; set; }

    [Column("dsClassificacaoConta")]
    [Required(ErrorMessage = "A descrição da classificação é obrigatória.")]
    [MaxLength(100)]
    public string DsClassificacaoConta { get; set; } = string.Empty;

    [Column("flAtivo")]
    public bool FlAtivo { get; set; } = true;

    [Column("dtCriacao")]
    public DateTime DtCriacao { get; set; } = DateTime.Now;

    [Column("dtEdicao")]
    public DateTime? DtEdicao { get; set; }
}
