using WebAppERP.Models;

namespace WebAppERP.DTOs;

// Item enviado pela tela (totais sao recalculados no servidor por seguranca).
public class VendaItemDto
{
    public string TpItem { get; set; } = "SERVICO"; // SERVICO | PRODUTO
    public int? IdServico { get; set; }
    public int? IdProduto { get; set; }
    public decimal QtItem { get; set; } = 1;
    public decimal VlUnitario { get; set; }
    public decimal VlDesconto { get; set; }
}

// Payload completo da venda (cabecalho + itens).
public class VendaInputDto
{
    public int IdVenda { get; set; }
    public int? IdCliente { get; set; }
    public int IdFuncionario { get; set; }
    public int? IdAgendamento { get; set; }
    public int? IdFormaPagamento { get; set; }
    public string TpStatus { get; set; } = "ABERTA"; // ABERTA | FINALIZADA | CANCELADA
    public decimal VlDesconto { get; set; }          // desconto geral
    public string? DsObservacao { get; set; }
    public List<VendaItemDto> Itens { get; set; } = [];
}

// Listas usadas para popular os combos da tela de venda.
public class VendaCombosDto
{
    public List<GerCliente> Clientes { get; set; } = [];
    public List<GerFuncionario> Funcionarios { get; set; } = [];
    public List<FinFormaPagamento> Formas { get; set; } = [];
    public List<OpeAgendamento> Agendamentos { get; set; } = [];
    public List<EstProduto> Produtos { get; set; } = [];
    public List<GerServico> Servicos { get; set; } = [];
}
