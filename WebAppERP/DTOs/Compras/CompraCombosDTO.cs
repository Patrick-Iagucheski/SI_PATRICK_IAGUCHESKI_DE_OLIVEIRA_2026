namespace WebAppERP.DTOs.Compras;

// Listas usadas para popular os combos da tela de compra.
public class CompraCombosDTO
{
    public List<Fornecedor> Fornecedores { get; set; } = [];
    public List<Transportador> Transportadores { get; set; } = [];
    public List<Produto> Produtos { get; set; } = [];
    public List<Servico> Servicos { get; set; } = [];
    public List<CondicaoPagamento> Condicoes { get; set; } = [];
    public List<FormaPagamento> Formas { get; set; } = [];
}
