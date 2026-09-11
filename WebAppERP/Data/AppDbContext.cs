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
    public DbSet<GerCargo> Cargos => Set<GerCargo>();
    public DbSet<GerServico> Servicos => Set<GerServico>();
    public DbSet<OpeAgendamento> Agendamentos => Set<OpeAgendamento>();
    public DbSet<OpeVenda> Vendas => Set<OpeVenda>();
    public DbSet<FinFormaPagamento> FormasPagamento => Set<FinFormaPagamento>();
    public DbSet<GerFornecedor> Fornecedores => Set<GerFornecedor>();
    public DbSet<GerTransportador> Transportadores => Set<GerTransportador>();
    public DbSet<GerVeiculo> Veiculos => Set<GerVeiculo>();
    public DbSet<EstProduto> Produtos => Set<EstProduto>();
    public DbSet<EstMarca> Marcas => Set<EstMarca>();
    public DbSet<EstCategoria> Categorias => Set<EstCategoria>();
    public DbSet<FinContaPagar> ContasAPagar => Set<FinContaPagar>();
    public DbSet<FinCondicaoPagamento> CondicoesPagamento => Set<FinCondicaoPagamento>();
    public DbSet<FinCondicaoPagamentoParcela> CondicoesPagamentoParcelas => Set<FinCondicaoPagamentoParcela>();
    public DbSet<OpeVendaItem> VendaItens => Set<OpeVendaItem>();
    public DbSet<FinClassificacaoConta> ClassificacoesConta => Set<FinClassificacaoConta>();
    public DbSet<OpeCompra> Compras => Set<OpeCompra>();
    public DbSet<OpeCompraItem> CompraItens => Set<OpeCompraItem>();
    public DbSet<OpeCompraPagamento> CompraPagamentos => Set<OpeCompraPagamento>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Parcelas seguem o ciclo de vida da condicao (exclusao em cascata).
        modelBuilder.Entity<FinCondicaoPagamento>()
            .HasMany(c => c.Parcelas)
            .WithOne(p => p.Condicao!)
            .HasForeignKey(p => p.IdCondicaoPagamento)
            .OnDelete(DeleteBehavior.Cascade);

        // A compra e identificada pela chave composta Numero + Serie + Modelo +
        // Fornecedor. Itens e parcelas referenciam essa chave COMPLETA (nao so o
        // numero da nota) e seguem o ciclo de vida da compra.
        modelBuilder.Entity<OpeCompra>()
            .HasMany(c => c.Itens)
            .WithOne(i => i.Compra!)
            .HasForeignKey(i => new { i.NrNota, i.NrSerie, i.NrModelo, i.IdFornecedor })
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OpeCompra>()
            .HasMany(c => c.Pagamentos)
            .WithOne(p => p.Compra!)
            .HasForeignKey(p => new { p.NrNota, p.NrSerie, p.NrModelo, p.IdFornecedor })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
