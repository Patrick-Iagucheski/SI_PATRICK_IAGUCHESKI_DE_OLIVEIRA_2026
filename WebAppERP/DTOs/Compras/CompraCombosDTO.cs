namespace WebAppERP.DTOs.Compras;

// Listas usadas para popular os combos da tela de compra. Fornecedor,
// transportador, produto e servico nao entram aqui: sao escolhidos pelas
// pesquisas AJAX da tela (OnGetBuscar...).
public class CompraCombosDTO
{
    public List<CondicaoPagamento> Condicoes { get; set; } = [];
    public List<FormaPagamento> Formas { get; set; } = [];
}
