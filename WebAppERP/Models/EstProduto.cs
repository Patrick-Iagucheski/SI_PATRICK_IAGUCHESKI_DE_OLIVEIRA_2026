using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppERP.Models;

[Table("tbEstProdutos")]
public class EstProduto
{
    [Key]
    [Column("idProduto")]
    public int IdProduto { get; set; }

    // Nome / descricao curta do produto (ex: "Pomada modeladora")
    [Column("dsProduto")]
    [Required, MaxLength(300)]
    public string DsProduto { get; set; } = string.Empty;

    // Marca: texto livre, sem tabela / sem FK
    [Column("nmMarca")]
    [MaxLength(60)]
    public string? NmMarca { get; set; }

    // Categoria: texto livre, sem tabela / sem FK
    [Column("dsCategoria")]
    [MaxLength(60)]
    public string? DsCategoria { get; set; }

    [Column("sgUnidade")]
    [MaxLength(10)]
    public string? SgUnidade { get; set; }

    [Column("vlCusto", TypeName = "decimal(12,2)")]
    public decimal? VlCusto { get; set; }

    [Column("vlVendaMinimo", TypeName = "decimal(12,2)")]
    public decimal? VlVendaMinimo { get; set; }

    [Column("vlVenda", TypeName = "decimal(12,2)")]
    public decimal? VlVenda { get; set; }

    [Column("pcComissao", TypeName = "decimal(5,2)")]
    public decimal? PcComissao { get; set; }

    [Column("vlDesconto", TypeName = "decimal(12,2)")]
    public decimal? VlDesconto { get; set; }

    // Saldo em estoque (usado tambem pela baixa de estoque nas Vendas)
    [Column("qtSaldo", TypeName = "decimal(12,2)")]
    public decimal? QtSaldo { get; set; }

    // Caminho relativo da imagem salva em wwwroot (ex: /uploads/xxx.png)
    [Column("dsImagem")]
    [MaxLength(260)]
    public string? DsImagem { get; set; }

    // Descricao longa / observacao do produto
    [Column("dsDescricao")]
    [MaxLength(500)]
    public string? DsDescricao { get; set; }

    [Column("flAtivo")]
    public bool FlAtivo { get; set; } = true;
}
