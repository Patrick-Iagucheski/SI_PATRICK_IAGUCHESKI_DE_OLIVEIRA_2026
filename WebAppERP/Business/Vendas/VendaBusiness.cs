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
// ou o estorno do estoque e os titulos do Contas a Receber. Regra nao atendida
// lanca ValidacaoException.
public class VendaBusiness
{
    private readonly AppDbContext _db;
    private readonly ClienteBusiness _cliente;
    private readonly FuncionarioBusiness _funcionario;
    private readonly FormaPagamentoBusiness _forma;
    private readonly AgendamentoBusiness _agendamento;
    private readonly ProdutoBusiness _produto;
    private readonly ServicoBusiness _servico;
    private readonly CondicaoPagamentoBusiness _condicao;
    private readonly ContaAReceberBusiness _contas;
    private readonly MovimentacaoEstoqueBusiness _movimentacao;

    public VendaBusiness(AppDbContext db, ClienteBusiness cliente, FuncionarioBusiness funcionario,
        FormaPagamentoBusiness forma, AgendamentoBusiness agendamento, ProdutoBusiness produto, ServicoBusiness servico,
        CondicaoPagamentoBusiness condicao, ContaAReceberBusiness contas, MovimentacaoEstoqueBusiness movimentacao)
    {
        _db = db;
        _cliente = cliente;
        _funcionario = funcionario;
        _forma = forma;
        _agendamento = agendamento;
        _produto = produto;
        _servico = servico;
        _condicao = condicao;
        _contas = contas;
        _movimentacao = movimentacao;
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
            .Include(v => v.CondicaoPagamento)
            .Include(v => v.Itens).ThenInclude(i => i.Servico)
            .Include(v => v.Itens).ThenInclude(i => i.Produto)
            .FirstOrDefaultAsync(v => v.IdVenda == id);

    // Situacao gravada da venda. Venda ainda nao gravada e aberta; null: nao existe mais.
    public async Task<string?> GetSituacao(int? id) =>
        id is not > 0
            ? Venda.ABERTA
            : await _db.Vendas.Where(v => v.IdVenda == id).Select(v => v.TpStatus).FirstOrDefaultAsync();

    // Listas da tela, cada uma vinda do Business da propria entidade (so ativos).
    public async Task<VendaCombosDTO> GetCombos() => new()
    {
        Clientes = await _cliente.Listar(),
        Funcionarios = await _funcionario.Listar(),
        Formas = await _forma.Listar(somenteAtivas: true),
        Condicoes = await _condicao.Listar(somenteAtivas: true),
        Agendamentos = await _agendamento.ListarParaVenda(),
        Produtos = await _produto.Listar(),
        Servicos = await _servico.Listar(somenteAtivos: true)
    };

    // ============================================================
    // Gravacao
    // ============================================================

    // Salvamento automatico da tela: so grava venda que continua aberta, para
    // uma tela antiga nao mexer numa venda finalizada ou cancelada em outra aba.
    public async Task<int> SalvarAberta(VendaDTO input)
    {
        var situacao = await GetSituacao(input.IdVenda)
            ?? throw new ValidacaoException("Esta venda não foi encontrada.", "Recarregue a tela.");
        if (situacao != Venda.ABERTA)
            throw new ValidacaoException("Só vendas abertas são salvas automaticamente.");

        input.TpStatus = Venda.ABERTA;
        return await Salvar(input);
    }

    // Inclui (IdVenda = 0) ou altera a venda aberta e devolve o id gravado.
    // ABERTA so grava; FINALIZADA exige itens e forma ou condicao de pagamento,
    // baixa o estoque e gera os titulos no Contas a Receber. Venda finalizada ou
    // cancelada nao e alterada: corrige-se cancelando e lancando uma nova.
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
            // Duas abas finalizando a mesma venda: a segunda espera a primeira
            // terminar e ja le a venda finalizada.
            await TravarVenda(input.IdVenda);
            existente = await _db.Vendas.Include(v => v.Itens).FirstOrDefaultAsync(v => v.IdVenda == input.IdVenda)
                ?? throw new ValidacaoException("Venda não encontrada.");
            if (existente.TpStatus == Venda.CANCELADA)
                throw new ValidacaoException("Venda cancelada não pode ser alterada.");
            if (existente.TpStatus == Venda.FINALIZADA)
                throw new ValidacaoException("Venda finalizada não pode ser alterada.",
                                             "Para corrigir, cancele a venda e lance uma nova.");
        }

        var agendamento = await ValidarCabecalho(input, existente, finalizar);
        await ValidarItensCadastrados(input, existente);
        var parcelas = await ParcelasDaCondicao(input, existente, finalizar);

        // Recalcula os itens e os totais no servidor (fonte da verdade).
        var (novosItens, subtotal) = MontarItens(input);

        // O desconto geral nao pode zerar a venda. So vale ao finalizar: a venda
        // aberta grava mesmo com o desconto digitado antes dos itens (o total
        // fica zerado ate os itens passarem do desconto).
        if (finalizar && input.VlDesconto > 0 && input.VlDesconto >= subtotal)
            throw new ValidacaoException($"O desconto da venda (R$ {input.VlDesconto:N2}) não pode ser igual ou maior "
                                       + $"que o subtotal (R$ {subtotal:N2}).");
        var total = Math.Max(subtotal - input.VlDesconto, 0);

        if (finalizar && novosItens.Count == 0)
            throw new ValidacaoException("Adicione ao menos um item para finalizar a venda.");

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

            // A venda aberta pode ter sido comecada antes: a venda acontece ao finalizar.
            if (finalizar)
                venda.DtVenda = DateTime.Now;
        }

        // Ao finalizar: valida e baixa o estoque dos produtos (a movimentacao
        // aponta para a venda, por isso depois de ela existir).
        if (finalizar)
            await AjustarEstoque(novosItens, -1, venda, validar: true);

        venda.IdCliente = input.IdCliente is > 0 ? input.IdCliente : null;
        venda.IdFuncionario = input.IdFuncionario;
        venda.IdAgendamento = input.IdAgendamento is > 0 ? input.IdAgendamento : null;
        // Com condicao, a forma vem das parcelas dela.
        venda.IdFormaPagamento = parcelas.Count > 0 ? parcelas[0].IdFormaPagamento
                               : input.IdFormaPagamento is > 0 ? input.IdFormaPagamento : null;
        venda.IdCondicaoPagamento = input.IdCondicaoPagamento is > 0 ? input.IdCondicaoPagamento : null;
        venda.TpStatus = input.TpStatus;
        venda.VlSubtotal = subtotal;
        venda.VlDesconto = input.VlDesconto;
        venda.VlTotal = total;
        venda.DsObservacao = observacao;
        venda.Itens = novosItens;

        // Venda finalizada: o atendimento do agendamento aconteceu.
        if (finalizar && agendamento != null && agendamento.TpStatus != Agendamento.CANCELADO)
            agendamento.TpStatus = Agendamento.CONCLUIDO;

        await _db.SaveChangesAsync();

        // Os titulos precisam do numero da venda ja gravado.
        if (finalizar && total > 0)
        {
            _contas.GerarDaVenda(venda, parcelas);
            await _db.SaveChangesAsync();
        }

        await tx.CommitAsync();
        return venda.IdVenda;
    }

    // ============================================================
    // Cancelamento e exclusao
    // ============================================================

    // Cancela a venda devolvendo ao estoque o que ela tinha baixado e cancelando
    // os titulos. Parcela a prazo ja recebida impede o cancelamento.
    // Devolve a mensagem para a tela: so a finalizada tinha estoque e titulos.
    public async Task<string> Cancelar(int id)
    {
        // Travada antes de ler: cancelamento e finalizacao da mesma venda ao
        // mesmo tempo esperam um pelo outro.
        await using var tx = await _db.Database.BeginTransactionAsync();
        await TravarVenda(id);
        var venda = await _db.Vendas.Include(v => v.Itens).FirstOrDefaultAsync(v => v.IdVenda == id)
            ?? throw new ValidacaoException("Venda não encontrada.");
        if (venda.TpStatus == Venda.CANCELADA)
            return "Esta venda já está cancelada.";

        var finalizada = venda.TpStatus == Venda.FINALIZADA;
        if (finalizada)
            await AjustarEstoque(venda.Itens, +1, venda); // devolve o estoque

        await _contas.CancelarDaVenda(id);
        venda.TpStatus = Venda.CANCELADA;
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return finalizada ? "Venda cancelada. Estoque devolvido e títulos do Contas a Receber cancelados."
                          : "Venda cancelada.";
    }

    // ============================================================
    // Apoio
    // ============================================================

    // Trava a linha da venda ate o fim da transacao.
    private Task TravarVenda(int idVenda) =>
        _db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM dbo.tbVenVendas WITH (UPDLOCK, ROWLOCK) WHERE idVenda = {idVenda}");

    // Funcionario, cliente, agendamento e forma de pagamento: precisam existir;
    // inativos so sao aceitos se ja eram os da venda (alteracao). Devolve o
    // agendamento da venda (null sem agendamento).
    private async Task<Agendamento?> ValidarCabecalho(VendaDTO input, Venda? existente, bool finalizar)
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

        Agendamento? agendamento = null;
        if (input.IdAgendamento is int idAgendamento && idAgendamento > 0)
        {
            // Travado: duas vendas nao pegam o mesmo agendamento ao mesmo tempo.
            await _db.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT 1 FROM dbo.tbOpeAgendamentos WITH (UPDLOCK, ROWLOCK) WHERE idAgendamento = {idAgendamento}");
            agendamento = await _db.Agendamentos.Include(a => a.Cliente).FirstOrDefaultAsync(a => a.IdAgendamento == idAgendamento)
                ?? throw new ValidacaoException("Agendamento informado não existe.");
            var jaEraDaVenda = existente?.IdAgendamento == idAgendamento;
            if (agendamento.TpStatus == Agendamento.CANCELADO && !jaEraDaVenda)
                throw new ValidacaoException("O agendamento selecionado está cancelado.");

            // Um atendimento e cobrado uma vez: venda cancelada libera o agendamento.
            if (!jaEraDaVenda)
            {
                var outraVenda = await VendaDoAgendamento(idAgendamento, input.IdVenda);
                if (outraVenda != null)
                    throw new ValidacaoException($"O agendamento #{idAgendamento} já está na venda Nº {outraVenda}.",
                                                 "Escolha outro agendamento ou continue a venda Nº " + outraVenda + ".");
            }

            if (input.IdCliente is > 0 && input.IdCliente != agendamento.IdCliente)
                throw new ValidacaoException($"O agendamento #{idAgendamento} é do cliente {agendamento.Cliente?.NmCliente}.",
                                             "Selecione o mesmo cliente na venda ou escolha outro agendamento.");
        }

        if (input.IdFormaPagamento is > 0)
        {
            var forma = await _db.FormasPagamento.FindAsync(input.IdFormaPagamento)
                ?? throw new ValidacaoException("Forma de pagamento informada não existe.");
            if (!forma.FlAtivo && existente?.IdFormaPagamento != forma.IdFormaPagamento)
                throw new ValidacaoException("A forma de pagamento selecionada está inativa.");
        }
        else if (finalizar && input.IdCondicaoPagamento is not > 0)
        {
            throw new ValidacaoException("Selecione a forma ou a condição de pagamento para finalizar a venda.");
        }

        return agendamento;
    }

    // Outra venda (aberta ou finalizada) que ja usa o agendamento, ou null.
    private Task<int?> VendaDoAgendamento(int idAgendamento, int idVendaAtual) =>
        _db.Vendas.Where(v => v.IdAgendamento == idAgendamento && v.IdVenda != idVendaAtual && v.TpStatus != Venda.CANCELADA)
                  .Select(v => (int?)v.IdVenda).FirstOrDefaultAsync();

    // Agendamento da venda (com o cliente, para o texto do campo), mesmo fora da
    // lista do campo: mais antigo, cancelado depois ou de outro dia.
    public async Task<Agendamento?> GetAgendamento(int? id) =>
        id is > 0
            ? await _db.Agendamentos.Include(a => a.Cliente).FirstOrDefaultAsync(a => a.IdAgendamento == id)
            : null;

    // Tela reexibida depois de uma gravacao que falhou: os vinculos da venda e
    // dos itens vem pelo id, inclusive os inativos (as listas da tela so tem
    // ativos), para nada sumir do formulario nem se perder no proximo salvamento.
    public async Task CarregarVinculos(Venda venda, List<VendaItem> itens)
    {
        venda.Cliente = venda.IdCliente is int idCliente ? await _db.Clientes.FindAsync(idCliente) : null;
        venda.Funcionario = await _db.Funcionarios.FindAsync(venda.IdFuncionario);
        venda.FormaPagamento = venda.IdFormaPagamento is int idForma ? await _db.FormasPagamento.FindAsync(idForma) : null;
        venda.CondicaoPagamento = venda.IdCondicaoPagamento is int idCondicao ? await _db.CondicoesPagamento.FindAsync(idCondicao) : null;
        foreach (var item in itens)
        {
            item.Produto = item.IdProduto is int idProduto ? await _db.Produtos.FindAsync(idProduto) : null;
            item.Servico = item.IdServico is int idServico ? await _db.Servicos.FindAsync(idServico) : null;
        }
    }

    // Produto e servico dos itens precisam existir. Inativo so continua se ja
    // era item da venda (inativado depois). Item novo inativo e recusado (ex.:
    // tela aberta antes da inativacao).
    private async Task ValidarItensCadastrados(VendaDTO input, Venda? existente)
    {
        var produtosDaVenda = existente?.Itens.Where(i => i.IdProduto != null).Select(i => i.IdProduto!.Value).ToHashSet() ?? [];
        var servicosDaVenda = existente?.Itens.Where(i => i.IdServico != null).Select(i => i.IdServico!.Value).ToHashSet() ?? [];

        var idsProdutos = input.Itens.Where(i => i.TpItem == VendaItem.PRODUTO && i.IdProduto is > 0)
                                     .Select(i => i.IdProduto!.Value).Distinct().ToList();
        var produtos = await _db.Produtos.Where(p => idsProdutos.Contains(p.IdProduto))
                                         .Select(p => new { p.IdProduto, p.DsProduto, p.FlAtivo }).ToListAsync();
        var idsServicos = input.Itens.Where(i => i.TpItem == VendaItem.SERVICO && i.IdServico is > 0)
                                     .Select(i => i.IdServico!.Value).Distinct().ToList();
        var servicos = await _db.Servicos.Where(s => idsServicos.Contains(s.IdServico))
                                         .Select(s => new { s.IdServico, s.DsServico, s.FlAtivo }).ToListAsync();

        var seq = 0;
        foreach (var item in input.Itens)
        {
            seq++;
            if (item.TpItem == VendaItem.PRODUTO && item.IdProduto is int idProduto && idProduto > 0)
            {
                var produto = produtos.FirstOrDefault(p => p.IdProduto == idProduto)
                    ?? throw new ValidacaoException($"Item {seq}: o produto código {idProduto} não foi encontrado.", "Atualize a tela e escolha o produto de novo.");
                if (!produto.FlAtivo && !produtosDaVenda.Contains(idProduto))
                    throw new ValidacaoException($"O produto \"{produto.DsProduto}\" está inativo.", "Escolha um produto ativo ou reative o cadastro.");
            }
            else if (item.TpItem == VendaItem.SERVICO && item.IdServico is int idServico && idServico > 0)
            {
                var servico = servicos.FirstOrDefault(s => s.IdServico == idServico)
                    ?? throw new ValidacaoException($"Item {seq}: o serviço código {idServico} não foi encontrado.", "Atualize a tela e escolha o serviço de novo.");
                if (!servico.FlAtivo && !servicosDaVenda.Contains(idServico))
                    throw new ValidacaoException($"O serviço \"{servico.DsServico}\" está inativo.", "Escolha um serviço ativo ou reative o cadastro.");
            }
        }
    }

    // Condicao cadastrada no cliente, sugerida ao escolher o cliente na venda.
    public Task<CondicaoPagamento?> GetCondicaoDoCliente(int idCliente) =>
        _cliente.GetCondicaoPagamento(idCliente);

    // Parcelas da condicao de pagamento escolhida (vazia sem condicao: a vista).
    // Venda com parcela a prazo precisa de cliente, para saber de quem cobrar.
    private async Task<List<CondicaoPagamentoParcela>> ParcelasDaCondicao(VendaDTO input, Venda? existente, bool finalizar)
    {
        if (input.IdCondicaoPagamento is not int idCondicao || idCondicao <= 0)
            return [];

        var condicao = await _db.CondicoesPagamento.FindAsync(idCondicao)
            ?? throw new ValidacaoException("Condição de pagamento informada não existe.");
        if (!condicao.FlAtivo && existente?.IdCondicaoPagamento != condicao.IdCondicaoPagamento)
            throw new ValidacaoException("A condição de pagamento selecionada está inativa.");

        var parcelas = await _condicao.GetParcelas(idCondicao);
        if (parcelas.Count == 0)
            throw new ValidacaoException("Esta condição de pagamento não possui parcelas cadastradas.");
        // Condicao gravada antes do limite de prazo: o vencimento nao caberia numa data.
        if (parcelas.Any(p => p.NrDiasVencimento > CondicaoPagamentoBusiness.DiasMaximos))
            throw new ValidacaoException($"A condição \"{condicao.DsCondicao}\" tem parcela com mais de {CondicaoPagamentoBusiness.DiasMaximos} dias.",
                                         "Corrija os dias na condição de pagamento ou escolha outra.");
        if (finalizar && input.IdCliente is not > 0 && parcelas.Any(p => p.NrDiasVencimento > 0))
            throw new ValidacaoException("Venda a prazo exige cliente.", "Selecione o cliente ou use uma condição à vista.");

        return parcelas;
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
    private async Task AjustarEstoque(IEnumerable<VendaItem> itens, int sinal, Venda venda, bool validar = false)
    {
        foreach (var item in itens.Where(i => i.TpItem == VendaItem.PRODUTO && i.IdProduto != null))
        {
            var prod = await _movimentacao.ProdutoTravado(item.IdProduto!.Value);
            if (prod == null) continue;

            var atual = prod.QtSaldo ?? 0;
            var novo = atual + sinal * item.QtItem;

            if (validar && novo < 0)
                throw new ValidacaoException($"Estoque insuficiente para '{prod.DsProduto}'. "
                                           + $"Saldo atual: {atual:N4}, solicitado: {item.QtItem:N4}.");

            // sinal +1 devolve o que a venda tinha baixado (estorno).
            await _movimentacao.Registrar(prod, sinal * item.QtItem, MovimentacaoEstoque.VENDA, estorno: sinal > 0,
                                    prod.VlCusto, venda: venda);
        }
    }
}
