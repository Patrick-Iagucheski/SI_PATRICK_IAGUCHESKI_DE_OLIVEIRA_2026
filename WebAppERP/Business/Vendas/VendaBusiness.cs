using Microsoft.EntityFrameworkCore;
using WebAppERP.Business.Estoque;
using WebAppERP.Business.Financeiro;
using WebAppERP.Business.Gerais;
using WebAppERP.Business.Operacional;
using WebAppERP.Data;
using WebAppERP.DTOs.Vendas;

namespace WebAppERP.Business.Vendas;

// Regras de negocio da venda (consultas, gravacao, cancelamento e exclusao).
// Gravacao, cancelamento e exclusao acontecem em transacao junto com a baixa
// ou o estorno do estoque. Regra nao atendida lanca ValidacaoException.
public class VendaBusiness
{
    private readonly AppDbContext _db;
    private readonly ClienteBusiness _cliente;
    private readonly FuncionarioBusiness _funcionario;
    private readonly FormaPagamentoBusiness _forma;
    private readonly AgendamentoBusiness _agendamento;
    private readonly ProdutoBusiness _produto;
    private readonly ServicoBusiness _servico;

    public VendaBusiness(AppDbContext db, ClienteBusiness cliente, FuncionarioBusiness funcionario,
        FormaPagamentoBusiness forma, AgendamentoBusiness agendamento, ProdutoBusiness produto, ServicoBusiness servico)
    {
        _db = db;
        _cliente = cliente;
        _funcionario = funcionario;
        _forma = forma;
        _agendamento = agendamento;
        _produto = produto;
        _servico = servico;
    }

    // ============================================================
    // Consultas
    // ============================================================

    public async Task<List<Venda>> Listar() =>
        await _db.Vendas
            .Include(v => v.Cliente)
            .Include(v => v.Funcionario)
            .OrderByDescending(v => v.DtVenda)
            .ToListAsync();

    public async Task<Venda?> GetVendaComItens(int id) =>
        await _db.Vendas
            .Include(v => v.Cliente)
            .Include(v => v.Funcionario)
            .Include(v => v.FormaPagamento)
            .Include(v => v.Itens).ThenInclude(i => i.Servico)
            .Include(v => v.Itens).ThenInclude(i => i.Produto)
            .FirstOrDefaultAsync(v => v.IdVenda == id);

    // Listas da tela, cada uma vinda do Business da propria entidade (so ativos).
    public async Task<VendaCombosDTO> GetCombos() => new()
    {
        Clientes = await _cliente.Listar(),
        Funcionarios = await _funcionario.Listar(),
        Formas = await _forma.Listar(somenteAtivas: true),
        Agendamentos = await _agendamento.ListarParaVenda(),
        Produtos = await _produto.Listar(),
        Servicos = await _servico.Listar(somenteAtivos: true)
    };

    // ============================================================
    // Gravacao
    // ============================================================

    // Inclui (IdVenda = 0) ou altera a venda e devolve o id gravado. ABERTA so
    // grava; FINALIZADA exige itens e forma de pagamento e baixa o estoque.
    public async Task<int> Salvar(VendaDTO input)
    {
        if (input.TpStatus is not (Venda.ABERTA or Venda.FINALIZADA))
            throw new ValidacaoException("Situação da venda inválida.", "Para cancelar, use o botão Cancelar da lista de vendas.");

        var finalizar = input.TpStatus == Venda.FINALIZADA;
        var observacao = string.IsNullOrWhiteSpace(input.DsObservacao) ? null : input.DsObservacao.Trim();
        if (observacao?.Length > 500)
            throw new ValidacaoException("A observação deve ter no máximo 500 caracteres.");
        if (input.VlDesconto < 0)
            throw new ValidacaoException("O desconto da venda não pode ser negativo.");

        // Qualquer ValidacaoException daqui em diante descarta a transacao
        // (rollback no dispose): o estoque nao e alterado.
        await using var tx = await _db.Database.BeginTransactionAsync();

        Venda? existente = null;
        if (input.IdVenda > 0)
        {
            existente = await _db.Vendas.Include(v => v.Itens).FirstOrDefaultAsync(v => v.IdVenda == input.IdVenda)
                ?? throw new ValidacaoException("Venda não encontrada.", "Ela pode ter sido excluída por outra pessoa.");
            if (existente.TpStatus == Venda.CANCELADA)
                throw new ValidacaoException("Venda cancelada não pode ser editada.");
        }

        await ValidarCabecalho(input, existente, finalizar);

        // Se a venda ja estava finalizada, estorna o estoque dos itens antigos antes de recalcular.
        if (existente is { TpStatus: Venda.FINALIZADA })
            await AjustarEstoque(existente.Itens, +1);

        // Recalcula os itens e os totais no servidor (fonte da verdade).
        var (novosItens, subtotal) = MontarItens(input);

        if (input.VlDesconto > subtotal)
            throw new ValidacaoException($"O desconto da venda (R$ {input.VlDesconto:N2}) é maior que o subtotal "
                                       + $"(R$ {subtotal:N2}) e deixaria o total negativo.");
        var total = subtotal - input.VlDesconto;

        // Ao finalizar: exige itens e valida/baixa o estoque dos produtos.
        if (finalizar)
        {
            if (novosItens.Count == 0)
                throw new ValidacaoException("Adicione ao menos um item para finalizar a venda.");

            await AjustarEstoque(novosItens, -1, validar: true);
        }

        Venda venda;
        if (existente == null)
        {
            venda = new Venda { DtVenda = DateTime.Now };
            _db.Vendas.Add(venda);
        }
        else
        {
            venda = existente;
            _db.VendaItens.RemoveRange(existente.Itens);
        }

        venda.IdCliente = input.IdCliente is > 0 ? input.IdCliente : null;
        venda.IdFuncionario = input.IdFuncionario;
        venda.IdAgendamento = input.IdAgendamento is > 0 ? input.IdAgendamento : null;
        venda.IdFormaPagamento = input.IdFormaPagamento is > 0 ? input.IdFormaPagamento : null;
        venda.TpStatus = input.TpStatus;
        venda.VlSubtotal = subtotal;
        venda.VlDesconto = input.VlDesconto;
        venda.VlTotal = total;
        venda.DsObservacao = observacao;
        venda.Itens = novosItens;

        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return venda.IdVenda;
    }

    // ============================================================
    // Cancelamento e exclusao
    // ============================================================

    // Cancela a venda devolvendo ao estoque o que ela tinha baixado.
    public async Task Cancelar(int id)
    {
        var venda = await _db.Vendas.Include(v => v.Itens).FirstOrDefaultAsync(v => v.IdVenda == id)
            ?? throw new ValidacaoException("Venda não encontrada.");
        if (venda.TpStatus == Venda.CANCELADA)
            return;

        await using var tx = await _db.Database.BeginTransactionAsync();
        if (venda.TpStatus == Venda.FINALIZADA)
            await AjustarEstoque(venda.Itens, +1); // devolve o estoque

        venda.TpStatus = Venda.CANCELADA;
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    // Exclusao definitiva. Venda finalizada movimentou estoque: so pelo cancelamento,
    // para o historico continuar mostrando a venda (mesma regra da Compra).
    public async Task Excluir(int id)
    {
        var venda = await _db.Vendas.Include(v => v.Itens).FirstOrDefaultAsync(v => v.IdVenda == id);
        if (venda == null)
            return;

        if (venda.TpStatus == Venda.FINALIZADA)
            throw new ValidacaoException("Venda finalizada não pode ser excluída.", "Utilize o cancelamento.");

        try
        {
            _db.VendaItens.RemoveRange(venda.Itens);
            _db.Vendas.Remove(venda);
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new ValidacaoException("Não é possível excluir: existem registros vinculados a esta venda.",
                                         "Utilize o cancelamento.");
        }
    }

    // ============================================================
    // Apoio
    // ============================================================

    // Funcionario, cliente, agendamento e forma de pagamento: precisam existir;
    // inativos so sao aceitos se ja eram os da venda (alteracao).
    private async Task ValidarCabecalho(VendaDTO input, Venda? existente, bool finalizar)
    {
        var funcionario = input.IdFuncionario > 0 ? await _db.Funcionarios.FindAsync(input.IdFuncionario) : null;
        if (funcionario == null)
            throw new ValidacaoException("Selecione o funcionário responsável pela venda.");
        if (!funcionario.FlAtivo && existente?.IdFuncionario != funcionario.IdFuncionario)
            throw new ValidacaoException("O funcionário selecionado está inativo.");

        if (input.IdCliente is > 0)
        {
            var cliente = await _db.Clientes.FindAsync(input.IdCliente)
                ?? throw new ValidacaoException("Cliente informado não existe.");
            if (!cliente.FlAtivo && existente?.IdCliente != cliente.IdCliente)
                throw new ValidacaoException("O cliente selecionado está inativo.");
        }

        if (input.IdAgendamento is > 0)
        {
            var agendamento = await _db.Agendamentos.FindAsync(input.IdAgendamento)
                ?? throw new ValidacaoException("Agendamento informado não existe.");
            if (agendamento.TpStatus == Agendamento.CANCELADO && existente?.IdAgendamento != agendamento.IdAgendamento)
                throw new ValidacaoException("O agendamento selecionado está cancelado.");
        }

        if (input.IdFormaPagamento is > 0)
        {
            var forma = await _db.FormasPagamento.FindAsync(input.IdFormaPagamento)
                ?? throw new ValidacaoException("Forma de pagamento informada não existe.");
            if (!forma.FlAtivo && existente?.IdFormaPagamento != forma.IdFormaPagamento)
                throw new ValidacaoException("A forma de pagamento selecionada está inativa.");
        }
        else if (finalizar)
        {
            throw new ValidacaoException("Selecione a forma de pagamento para finalizar a venda.");
        }
    }

    // Recalcula os itens no servidor. O total enviado pela tela e ignorado.
    private static (List<VendaItem> itens, decimal subtotal) MontarItens(VendaDTO input)
    {
        var itens = new List<VendaItem>();
        decimal subtotal = 0;
        var seq = 0;

        foreach (var dto in input.Itens)
        {
            seq++;

            var produto = dto.TpItem == VendaItem.PRODUTO;
            if (!produto && dto.TpItem != VendaItem.SERVICO)
                throw new ValidacaoException($"Item {seq}: tipo de item inválido.");

            var totalItem = Module.CalcularTotalItem(seq, produto, dto.IdProduto, dto.IdServico,
                                                     dto.QtItem, dto.VlUnitario, dto.VlDesconto);
            subtotal += totalItem;

            itens.Add(new VendaItem
            {
                TpItem = produto ? VendaItem.PRODUTO : VendaItem.SERVICO,
                IdServico = produto ? null : dto.IdServico,
                IdProduto = produto ? dto.IdProduto : null,
                QtItem = dto.QtItem,
                VlUnitario = dto.VlUnitario,
                VlDesconto = dto.VlDesconto,
                VlTotal = totalItem
            });
        }

        return (itens, subtotal);
    }

    // Ajusta o saldo dos produtos. sinal = -1 (baixa) ou +1 (estorno).
    // validar=true impede saldo negativo (regra: produto nao pode ficar negativo).
    private async Task AjustarEstoque(IEnumerable<VendaItem> itens, int sinal, bool validar = false)
    {
        foreach (var item in itens.Where(i => i.TpItem == VendaItem.PRODUTO && i.IdProduto != null))
        {
            var prod = await _db.Produtos.FindAsync(item.IdProduto);
            if (prod == null) continue;

            var atual = prod.QtSaldo ?? 0;
            var novo = atual + sinal * item.QtItem;

            if (validar && novo < 0)
                throw new ValidacaoException($"Estoque insuficiente para '{prod.DsProduto}'. "
                                           + $"Saldo atual: {atual:N4}, solicitado: {item.QtItem:N4}.");

            prod.QtSaldo = novo;
        }
    }
}
