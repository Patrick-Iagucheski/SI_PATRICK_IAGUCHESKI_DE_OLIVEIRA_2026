namespace WebAppERP.DTOs.Vendas;

// Listas usadas para popular os combos da tela de venda.
public class VendaCombosDTO
{
    public List<Cliente> Clientes { get; set; } = [];
    public List<Funcionario> Funcionarios { get; set; } = [];
    public List<FormaPagamento> Formas { get; set; } = [];
    public List<Agendamento> Agendamentos { get; set; } = [];
    public List<Produto> Produtos { get; set; } = [];
    public List<Servico> Servicos { get; set; } = [];
}
