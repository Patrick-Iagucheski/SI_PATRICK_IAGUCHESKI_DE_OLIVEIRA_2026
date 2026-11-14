using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppERP.Models.Estoque;

// Cadastro: texto gravado em MAIUSCULO (CaixaAltaAttribute).
[CaixaAlta]
[Table("tbEstProdutos")]
public class Produto : EntidadeComRegistro
{
    [Key]
    [Column("idProduto")]
    public int IdProduto { get; set; }

    // Nome / descricao curta do produto (ex: "Pomada modeladora")
    [Nome]
    [Column("dsProduto")]
    [Required, MaxLength(100)]
    public string DsProduto { get; set; } = string.Empty;

    // Marca: vinculo com o cadastro de Marcas (tbEstMarcas).
    [Column("idMarca")]
    public int? IdMarca { get; set; }

    [ForeignKey("IdMarca")]
    public Marca? Marca { get; set; }

    // Categoria: vinculo com o cadastro de Categorias (tbEstCategorias).
    // Substituiu o antigo campo de texto livre dsCategoria.
    [Column("idCategoria")]
    public int? IdCategoria { get; set; }

    [ForeignKey("IdCategoria")]
    public Categoria? Categoria { get; set; }

    // Unidade de medida: vinculo com o cadastro de Unidades de Medida
    // (tbEstUnidadesMedida). Substituiu o antigo texto tbEstProdutos.sgUnidade.
    [Column("idUnidadeMedida")]
    public int? IdUnidadeMedida { get; set; }

    [ForeignKey("IdUnidadeMedida")]
    public UnidadeMedida? UnidadeMedida { get; set; }

    // Custo medio (6 casas), calculado pela entrada da compra (CompraBusiness).
    [Column("vlCusto", TypeName = "decimal(18,6)")]
    public decimal? VlCusto { get; set; }

    [Column("vlVendaMinimo", TypeName = "decimal(12,2)")]
    public decimal? VlVendaMinimo { get; set; }

    [Column("vlVenda", TypeName = "decimal(12,2)")]
    public decimal? VlVenda { get; set; }

    [Column("pcComissao", TypeName = "decimal(5,2)")]
    public decimal? PcComissao { get; set; }

    [Column("vlDesconto", TypeName = "decimal(12,2)")]
    public decimal? VlDesconto { get; set; }

    // Saldo em estoque (usado tambem pela baixa de estoque nas Vendas). 4 casas,
    // como a coluna e as quantidades dos itens e da movimentacao.
    [Column("qtSaldo", TypeName = "decimal(12,4)")]
    public decimal? QtSaldo { get; set; }

    // Caminho relativo da imagem salva em wwwroot (ex: /uploads/xxx.png)
    [Column("dsImagem")]
    [MaxLength(260)]
    [ManterCaixa]
    public string? DsImagem { get; set; }

    // Descricao longa / observacao do produto
    [MultiLinha]
    [Column("dsDescricao")]
    [MaxLength(500)]
    public string? DsDescricao { get; set; }

    [Column("flAtivo")]
    public bool FlAtivo { get; set; } = true;
}
