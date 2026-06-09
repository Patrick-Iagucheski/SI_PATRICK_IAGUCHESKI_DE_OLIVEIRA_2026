using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;
using WebAppERP.DTOs;
using WebAppERP.Models;

namespace WebAppERP.Repositories;

public class VendaRepository : IVendaRepository
{
    private readonly AppDbContext _db;

    public VendaRepository(AppDbContext db) => _db = db;

    public async Task<List<OpeVenda>> ListarAsync() =>
        await _db.Vendas
            .Include(v => v.Cliente)
            .Include(v => v.Funcionario)
            .OrderByDescending(v => v.DtVenda)
            .ToListAsync();

    public async Task<OpeVenda?> ObterComItensAsync(int id) =>
        await _db.Vendas
            .Include(v => v.Cliente)
            .Include(v => v.Funcionario)
            .Include(v => v.FormaPagamento)
            .Include(v => v.Itens).ThenInclude(i => i.Servico)
            .Include(v => v.Itens).ThenInclude(i => i.Produto)
            .FirstOrDefaultAsync(v => v.IdVenda == id);

    public async Task<VendaCombosDto> ObterCombosAsync() => new()
    {
        Clientes = await _db.Clientes.Where(c => c.FlAtivo).OrderBy(c => c.NmCliente).ToListAsync(),
        Funcionarios = await _db.Funcionarios.Where(f => f.FlAtivo).OrderBy(f => f.NmFuncionario).ToListAsync(),
        Formas = await _db.FormasPagamento.Where(f => f.FlAtivo).OrderBy(f => f.DsFormaPagamento).ToListAsync(),
        Agendamentos = await _db.Agendamentos
            .Include(a => a.Cliente)
            .Where(a => a.TpStatus != "CANCELADO")
            .OrderByDescending(a => a.DtAgendamento).ThenByDescending(a => a.HrInicio)
            .Take(100)
            .ToListAsync(),
        Produtos = await _db.Produtos.OrderBy(p => p.DsProduto).ToListAsync(),
        Servicos = await _db.Servicos.Where(s => s.FlAtivo).OrderBy(s => s.DsServico).ToListAsync()
    };

    public async Task<(bool ok, string? erro, int idVenda)> SalvarAsync(VendaInputDto input)
    {
        if (input.IdFuncionario <= 0)
            return (false, "Selecione o funcionario responsavel pela venda.", input.IdVenda);

        await using var tx = await _db.Database.BeginTransactionAsync();

        OpeVenda? existing = null;
        if (input.IdVenda > 0)
        {
            existing = await _db.Vendas.Include(v => v.Itens).FirstOrDefaultAsync(v => v.IdVenda == input.IdVenda);
            if (existing == null) return (false, "Venda nao encontrada.", 0);
            if (existing.TpStatus == "CANCELADA") return (false, "Venda cancelada nao pode ser editada.", input.IdVenda);
        }

        // Se a venda ja estava finalizada, estorna o estoque dos itens antigos antes de recalcular.
        if (existing is { TpStatus: "FINALIZADA" })
            await AjustarEstoqueAsync(existing.Itens, +1);

        // Recalcula os itens e os totais no servidor (fonte da verdade).
        var novosItens = new List<OpeVendaItem>();
        decimal subtotal = 0;
        foreach (var dto in input.Itens)
        {
            var totalItem = dto.QtItem * dto.VlUnitario - dto.VlDesconto;
            if (totalItem < 0) totalItem = 0;
            subtotal += totalItem;

            novosItens.Add(new OpeVendaItem
            {
                TpItem = dto.TpItem,
                IdServico = dto.TpItem == "SERVICO" ? dto.IdServico : null,
                IdProduto = dto.TpItem == "PRODUTO" ? dto.IdProduto : null,
                QtItem = dto.QtItem,
                VlUnitario = dto.VlUnitario,
                VlDesconto = dto.VlDesconto,
                VlTotal = totalItem
            });
        }

        var total = subtotal - input.VlDesconto;
        if (total < 0) total = 0;

        // Ao finalizar: exige itens e valida/baixa o estoque dos produtos.
        if (input.TpStatus == "FINALIZADA")
        {
            if (novosItens.Count == 0)
                return (false, "Adicione ao menos um item para finalizar a venda.", input.IdVenda);

            var (ok, erro) = await AjustarEstoqueAsync(novosItens, -1, validar: true);
            if (!ok) return (false, erro, input.IdVenda);
        }

        OpeVenda venda;
        if (existing == null)
        {
            venda = new OpeVenda { DtCriacao = DateTime.Now, DtVenda = DateTime.Now };
            _db.Vendas.Add(venda);
        }
        else
        {
            venda = existing;
            _db.VendaItens.RemoveRange(existing.Itens);
            venda.DtEdicao = DateTime.Now;
        }

        venda.IdCliente = input.IdCliente;
        venda.IdFuncionario = input.IdFuncionario;
        venda.IdAgendamento = input.IdAgendamento;
        venda.IdFormaPagamento = input.IdFormaPagamento;
        venda.TpStatus = input.TpStatus;
        venda.VlSubtotal = subtotal;
        venda.VlDesconto = input.VlDesconto;
        venda.VlTotal = total;
        venda.DsObservacao = input.DsObservacao;
        venda.Itens = novosItens;

        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return (true, null, venda.IdVenda);
    }

    public async Task<(bool ok, string? erro)> CancelarAsync(int id)
    {
        var venda = await _db.Vendas.Include(v => v.Itens).FirstOrDefaultAsync(v => v.IdVenda == id);
        if (venda == null) return (false, "Venda nao encontrada.");
        if (venda.TpStatus == "CANCELADA") return (true, null);

        await using var tx = await _db.Database.BeginTransactionAsync();
        if (venda.TpStatus == "FINALIZADA")
            await AjustarEstoqueAsync(venda.Itens, +1); // devolve o estoque

        venda.TpStatus = "CANCELADA";
        venda.DtEdicao = DateTime.Now;
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return (true, null);
    }

    public async Task ExcluirAsync(int id)
    {
        var venda = await _db.Vendas.Include(v => v.Itens).FirstOrDefaultAsync(v => v.IdVenda == id);
        if (venda == null) return;

        await using var tx = await _db.Database.BeginTransactionAsync();
        if (venda.TpStatus == "FINALIZADA")
            await AjustarEstoqueAsync(venda.Itens, +1);

        _db.VendaItens.RemoveRange(venda.Itens);
        _db.Vendas.Remove(venda);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    // Ajusta o saldo dos produtos. sinal = -1 (baixa) ou +1 (estorno).
    // validar=true impede saldo negativo (regra: produto nao pode ficar negativo).
    private async Task<(bool ok, string? erro)> AjustarEstoqueAsync(IEnumerable<OpeVendaItem> itens, int sinal, bool validar = false)
    {
        foreach (var item in itens.Where(i => i.TpItem == "PRODUTO" && i.IdProduto != null))
        {
            var prod = await _db.Produtos.FindAsync(item.IdProduto);
            if (prod == null) continue;

            var atual = prod.QtSaldo ?? 0;
            var novo = atual + sinal * item.QtItem;

            if (validar && novo < 0)
                return (false, $"Estoque insuficiente para '{prod.DsProduto}'. Saldo atual: {atual:N4}, solicitado: {item.QtItem:N4}.");

            prod.QtSaldo = novo;
        }
        return (true, null);
    }
}
