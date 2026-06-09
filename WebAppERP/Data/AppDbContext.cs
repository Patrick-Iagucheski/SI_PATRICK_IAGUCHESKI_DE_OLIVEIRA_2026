using Microsoft.EntityFrameworkCore;
using WebAppERP.Models;

namespace WebAppERP.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<GerUsuario> Usuarios => Set<GerUsuario>();
    public DbSet<GerPais> Paises => Set<GerPais>();
    public DbSet<GerEstado> Estados => Set<GerEstado>();
    public DbSet<GerCidade> Cidades => Set<GerCidade>();
    public DbSet<GerCliente> Clientes => Set<GerCliente>();
    public DbSet<GerFuncionario> Funcionarios => Set<GerFuncionario>();
    public DbSet<GerServico> Servicos => Set<GerServico>();
    public DbSet<OpeAgendamento> Agendamentos => Set<OpeAgendamento>();
    public DbSet<OpeVenda> Vendas => Set<OpeVenda>();
    public DbSet<FinFormaPagamento> FormasPagamento => Set<FinFormaPagamento>();
    public DbSet<GerFornecedor> Fornecedores => Set<GerFornecedor>();
    public DbSet<GerTransportador> Transportadores => Set<GerTransportador>();
    public DbSet<GerVeiculo> Veiculos => Set<GerVeiculo>();
    public DbSet<EstProduto> Produtos => Set<EstProduto>();
    public DbSet<FinContaPagar> ContasAPagar => Set<FinContaPagar>();
    public DbSet<OpeVendaItem> VendaItens => Set<OpeVendaItem>();
}
