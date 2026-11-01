using Microsoft.EntityFrameworkCore;

namespace WebAppERP.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Empresa> Empresa => Set<Empresa>();
    public DbSet<Pais> Paises => Set<Pais>();
    public DbSet<Estado> Estados => Set<Estado>();
    public DbSet<Cidade> Cidades => Set<Cidade>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Funcionario> Funcionarios => Set<Funcionario>();
    public DbSet<Cargo> Cargos => Set<Cargo>();
    public DbSet<Servico> Servicos => Set<Servico>();
    public DbSet<Agendamento> Agendamentos => Set<Agendamento>();
    public DbSet<Venda> Vendas => Set<Venda>();
    public DbSet<FormaPagamento> FormasPagamento => Set<FormaPagamento>();
    public DbSet<Fornecedor> Fornecedores => Set<Fornecedor>();
    public DbSet<Transportador> Transportadores => Set<Transportador>();
    public DbSet<Veiculo> Veiculos => Set<Veiculo>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Marca> Marcas => Set<Marca>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<ContaAPagar> ContasAPagar => Set<ContaAPagar>();
    public DbSet<CondicaoPagamento> CondicoesPagamento => Set<CondicaoPagamento>();
    public DbSet<CondicaoPagamentoParcela> CondicoesPagamentoParcelas => Set<CondicaoPagamentoParcela>();
    public DbSet<VendaItem> VendaItens => Set<VendaItem>();
    public DbSet<ClassificacaoConta> ClassificacoesConta => Set<ClassificacaoConta>();
    public DbSet<Compra> Compras => Set<Compra>();
    public DbSet<CompraItem> CompraItens => Set<CompraItem>();
    public DbSet<CompraPagamento> CompraPagamentos => Set<CompraPagamento>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Parcelas seguem o ciclo de vida da condicao (exclusao em cascata).
        modelBuilder.Entity<CondicaoPagamento>()
            .HasMany(c => c.Parcelas)
            .WithOne(p => p.Condicao!)
            .HasForeignKey(p => p.IdCondicaoPagamento)
            .OnDelete(DeleteBehavior.Cascade);

        // A compra e identificada pela chave composta Numero + Serie + Modelo +
        // Fornecedor. Itens e parcelas referenciam essa chave COMPLETA (nao so o
        // numero da nota) e seguem o ciclo de vida da compra.
        modelBuilder.Entity<Compra>()
            .HasMany(c => c.Itens)
            .WithOne(i => i.Compra!)
            .HasForeignKey(i => new { i.NrNota, i.NrSerie, i.NrModelo, i.IdFornecedor })
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Compra>()
            .HasMany(c => c.Pagamentos)
            .WithOne(p => p.Compra!)
            .HasForeignKey(p => new { p.NrNota, p.NrSerie, p.NrModelo, p.IdFornecedor })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
