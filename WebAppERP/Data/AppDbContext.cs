using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using WebAppERP.Infrastructure;
using WebAppERP.Models;

namespace WebAppERP.Data;

public class AppDbContext : DbContext
{
    private readonly UsuarioAtual _usuario;

    public AppDbContext(DbContextOptions<AppDbContext> options, UsuarioAtual usuario) : base(options)
        => _usuario = usuario;

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
    public DbSet<UnidadeMedida> UnidadesMedida => Set<UnidadeMedida>();
    public DbSet<CategoriaServico> CategoriasServico => Set<CategoriaServico>();
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

        // Registro (quem incluiu / alterou): o nome dos usuarios vem junto em
        // toda consulta, para as telas mostrarem sem Include em cada busca.
        foreach (var tipo in modelBuilder.Model.GetEntityTypes()
                     .Where(t => typeof(EntidadeComRegistro).IsAssignableFrom(t.ClrType)).ToList())
        {
            var entidade = modelBuilder.Entity(tipo.ClrType);
            entidade.Navigation(nameof(EntidadeComRegistro.UsuarioInclusao)).AutoInclude();
            entidade.Navigation(nameof(EntidadeComRegistro.UsuarioEdicao)).AutoInclude();
        }

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

    // ============================================================
    // Registro de quem incluiu / alterou
    // ============================================================
    // Toda gravacao passa por aqui: inclusao grava o usuario e a data de
    // criacao; alteracao, o usuario e a data da edicao. Os Business nao
    // precisam (e nao devem) preencher essas colunas.
    private static readonly ConcurrentDictionary<string, int?> IdPorLogin = new();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ConverterCaixaAlta();
        PreencherRegistro(IdUsuarioAtual());
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ConverterCaixaAlta();
        PreencherRegistro(IdUsuarioAtual());
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    // Id do usuario atual (consulta uma vez por login; sem usuario cadastrado, fica sem).
    private int? IdUsuarioAtual()
    {
        if (!ChangeTracker.Entries<EntidadeComRegistro>().Any(e => e.State is EntityState.Added or EntityState.Modified))
            return null;
        if (IdPorLogin.TryGetValue(_usuario.Login, out var id) && id != null)
            return id;
        id = Usuarios.AsNoTracking().Where(u => u.DsLogin == _usuario.Login)
                     .Select(u => (int?)u.IdUsuario).FirstOrDefault();
        if (id != null) IdPorLogin[_usuario.Login] = id;
        return id;
    }

    private void PreencherRegistro(int? idUsuario)
    {
        var agora = DateTime.Now;
        foreach (var e in ChangeTracker.Entries<EntidadeComRegistro>())
        {
            if (e.State == EntityState.Added)
            {
                e.Entity.IdUsuario = idUsuario;
                e.Entity.DtCriacao = agora;
            }
            else if (e.State == EntityState.Modified)
            {
                e.Entity.IdUsuarioEdicao = idUsuario;
                e.Entity.DtEdicao = agora;
                // Quem incluiu nao muda numa alteracao.
                e.Property(x => x.IdUsuario).IsModified = false;
                e.Property(x => x.DtCriacao).IsModified = false;
            }
        }
    }

    // ============================================================
    // Cadastros em maiusculo
    // ============================================================
    // Entidade marcada com [CaixaAlta] (cadastros) grava todo texto em
    // MAIUSCULO, menos as propriedades [ManterCaixa] (e-mail, site, imagem,
    // chave PIX). Fica aqui, num lugar so, para valer em todo cadastro e em
    // toda forma de gravar (tela, modal, cadastro rapido).
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    private void ConverterCaixaAlta()
    {
        foreach (var e in ChangeTracker.Entries())
        {
            if (e.State is not (EntityState.Added or EntityState.Modified)) continue;
            if (!Attribute.IsDefined(e.Entity.GetType(), typeof(CaixaAltaAttribute))) continue;

            foreach (var p in e.Properties)
            {
                if (p.CurrentValue is not string texto) continue;
                if (p.Metadata.PropertyInfo is { } info && Attribute.IsDefined(info, typeof(ManterCaixaAttribute))) continue;

                var maiusculo = texto.ToUpper(PtBr);
                if (maiusculo != texto) p.CurrentValue = maiusculo;
            }
        }
    }
}
