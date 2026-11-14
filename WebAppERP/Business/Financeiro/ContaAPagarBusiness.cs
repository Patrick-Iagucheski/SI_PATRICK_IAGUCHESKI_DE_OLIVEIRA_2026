using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;

namespace WebAppERP.Business.Financeiro;

// Regras de negocio do titulo a pagar lancado ou mantido pela tela de Contas
// a Pagar. Titulos gerados pela compra (NrNota preenchido) podem receber
// baixa, mas valor, fornecedor, datas e exclusao so mudam pela propria compra.
public class ContaAPagarBusiness
{
    private readonly AppDbContext _db;

    public ContaAPagarBusiness(AppDbContext db) => _db = db;

    private static readonly string[] StatusValidos = [ContaAPagar.PENDENTE, ContaAPagar.PAGO, ContaAPagar.CANCELADO];

    // Situacao apresentada na tela: PAGO / CANCELADO / VENCIDA / PENDENTE.
    public static string GetSituacao(ContaAPagar c, DateOnly hoje)
    {
        if (c.DsStatus == ContaAPagar.PAGO) return ContaAPagar.PAGO;
        if (c.DsStatus == ContaAPagar.CANCELADO) return ContaAPagar.CANCELADO;
        if (c.DtVencimento != null && c.DtVencimento < hoje) return ContaAPagar.VENCIDA;
        return ContaAPagar.PENDENTE;
    }

    // ============================================================
    // Consultas
    // ============================================================

    public async Task<List<ContaAPagar>> Listar() =>
        await _db.ContasAPagar
            .Include(c => c.Fornecedor)
            .Include(c => c.FormaPagamento)
            .OrderBy(c => c.DtVencimento)
            .ThenBy(c => c.IdContaPagar)
            .ToListAsync();

    // Pesquisa da tela: por codigo, descricao, documento, fornecedor e situacao.
    //   situacao: PENDENTE (inclui as vencidas, padrao da tela), VENCIDA, PAGO,
    //             CANCELADO ou vazio (todas).
    public async Task<List<ContaAPagar>> Buscar(int? id, string? conta, string? documento, int? idFornecedor, string? situacao)
    {
        var query = _db.ContasAPagar
            .Include(c => c.Fornecedor)
            .Include(c => c.FormaPagamento)
            .AsQueryable();

        var hoje = DateOnly.FromDateTime(DateTime.Today);
        query = (situacao ?? "").Trim().ToUpper() switch
        {
            ContaAPagar.PENDENTE => query.Where(c => c.DsStatus == ContaAPagar.PENDENTE),
            ContaAPagar.VENCIDA => query.Where(c => c.DsStatus == ContaAPagar.PENDENTE && c.DtVencimento < hoje),
            ContaAPagar.PAGO => query.Where(c => c.DsStatus == ContaAPagar.PAGO),
            ContaAPagar.CANCELADO => query.Where(c => c.DsStatus == ContaAPagar.CANCELADO),
            _ => query
        };

        if (id.HasValue)
            query = query.Where(c => c.IdContaPagar == id.Value);

        if (!string.IsNullOrWhiteSpace(conta))
        {
            query = query.Where(Module.MontarFiltroTexto<ContaAPagar>(conta, c => c.DsConta));
        }

        if (!string.IsNullOrWhiteSpace(documento))
        {
            query = query.Where(Module.MontarFiltroTexto<ContaAPagar>(documento, c => c.NrDocumento));
        }

        if (idFornecedor.HasValue)
            query = query.Where(c => c.IdFornecedor == idFornecedor.Value);

        return await query.OrderBy(c => c.DtVencimento).ThenBy(c => c.IdContaPagar).Take(100).ToListAsync();
    }

    // ============================================================
    // Gravacao
    // ============================================================

    // Inclui (IdContaPagar = 0) ou altera o titulo e devolve o registro gravado.
    public async Task<ContaAPagar> Salvar(ContaAPagar conta)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();
        var status = (conta.DsStatus ?? ContaAPagar.PENDENTE).Trim().ToUpper();
        if (!StatusValidos.Contains(status)) status = ContaAPagar.PENDENTE;

        var descricao = Module.Truncar(conta.DsConta?.Trim(), 100) ?? string.Empty;
        var documento = string.IsNullOrWhiteSpace(conta.NrDocumento) ? null : Module.Truncar(conta.NrDocumento.Trim(), 20);
        var observacao = Module.Truncar(conta.DsObservacao, 500);

        if (descricao.Length == 0)
            throw new ValidacaoException("Informe a descrição da conta.");
        if (conta.IdFornecedor <= 0 || !await _db.Fornecedores.AnyAsync(f => f.IdFornecedor == conta.IdFornecedor))
            throw new ValidacaoException("Selecione o fornecedor da conta.");
        if (conta.IdFormaPagamento is int idForma && !await _db.FormasPagamento.AnyAsync(f => f.IdFormaPagamento == idForma))
            throw new ValidacaoException("Forma de pagamento inválida.");
        if (conta.VlValor <= 0)
            throw new ValidacaoException("Informe o valor da conta.");
        if (conta.VlPago < 0)
            throw new ValidacaoException("O valor pago não pode ser negativo.");
        if (conta.VlPago > conta.VlValor)
            throw new ValidacaoException("O valor pago não pode ser maior que o valor da conta.");

        // Situacao e pagamento andam juntos: Pago tem valor e data (sem informar,
        // o valor da conta e hoje); Pendente nao tem pagamento (voltar para
        // Pendente e o estorno do pagamento).
        // Pago e o valor total: pagamento parcial nao e registrado (a conta
        // continua Pendente ate pagar tudo).
        if (status == ContaAPagar.PAGO)
        {
            if (conta.VlPago == 0) conta.VlPago = conta.VlValor;
            if (conta.VlPago != conta.VlValor)
                throw new ValidacaoException($"Para marcar como Pago, o valor pago deve ser o total da conta (R$ {conta.VlValor:N2}).",
                                             "Pagamento parcial não é registrado: deixe a conta Pendente até pagar o total.");
            conta.DtPagamento ??= DateOnly.FromDateTime(DateTime.Today);
        }
        else if (status == ContaAPagar.PENDENTE)
        {
            conta.VlPago = 0;
            conta.DtPagamento = null;
        }
        Module.ValidarDatasTitulo(conta.DtEmissao, conta.DtVencimento, conta.DtPagamento, "pagamento");

        ContaAPagar registro;
        if (conta.IdContaPagar == 0)
        {
            registro = new ContaAPagar();
            _db.ContasAPagar.Add(registro);
        }
        else
        {
            // Linha travada ate gravar: baixa ou cancelamento ao mesmo tempo esperam.
            await _db.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT 1 FROM dbo.tbFinContasAPagar WITH (UPDLOCK, ROWLOCK) WHERE idContaPagar = {conta.IdContaPagar}");
            registro = await _db.ContasAPagar.FindAsync(conta.IdContaPagar)
                ?? throw new ValidacaoException("Conta a pagar não encontrada.", "Ela pode ter sido excluída por outra pessoa.");

            // Titulo da compra: so o pagamento (data, valor pago, situacao) e a
            // observacao mudam por aqui; o resto vem das parcelas da compra.
            if (registro.OriginadoDeCompra)
            {
                // Compra cancelada: o titulo fica como o cancelamento deixou
                // (cancelado), sem reabrir nem pagar.
                if (await CompraCancelada(registro))
                    throw new ValidacaoException($"Este título é da compra Nº {registro.NrNota}, que foi cancelada.",
                                                 "Título de compra cancelada não pode ser alterado.");
                if (registro.IdFornecedor != conta.IdFornecedor || registro.VlValor != conta.VlValor
                    || registro.DtEmissao != conta.DtEmissao || registro.DtVencimento != conta.DtVencimento
                    || registro.DsConta != descricao || registro.NrDocumento != documento)
                    throw new ValidacaoException($"Este título foi gerado pela compra Nº {registro.NrNota}.",
                                                 "Fornecedor, valor, datas e descrição só podem ser alterados pela própria compra.");
                if (status == ContaAPagar.CANCELADO && registro.DsStatus != ContaAPagar.CANCELADO)
                    throw new ValidacaoException($"Este título foi gerado pela compra Nº {registro.NrNota}.",
                                                 "Para cancelá-lo, cancele a compra.");
            }
        }

        // Fornecedor e forma precisam estar ativos no titulo novo ou quando sao
        // trocados; o que o titulo ja tinha fica, mesmo inativado depois (o
        // fornecedor inativo ainda pode ter parcela a pagar).
        if (conta.IdFornecedor != registro.IdFornecedor
            && !await _db.Fornecedores.AnyAsync(f => f.IdFornecedor == conta.IdFornecedor && f.FlAtivo))
            throw new ValidacaoException("O fornecedor selecionado está inativo.", "Escolha um fornecedor ativo ou reative o cadastro.");
        if (conta.IdFormaPagamento is int idFormaNova && conta.IdFormaPagamento != registro.IdFormaPagamento
            && !await _db.FormasPagamento.AnyAsync(f => f.IdFormaPagamento == idFormaNova && f.FlAtivo))
            throw new ValidacaoException("A forma de pagamento selecionada está inativa.", "Escolha uma forma ativa ou reative o cadastro.");

        registro.DsConta = descricao;
        registro.NrDocumento = documento;
        registro.IdFornecedor = conta.IdFornecedor;
        registro.IdFormaPagamento = conta.IdFormaPagamento;
        registro.DtEmissao = conta.DtEmissao;
        registro.DtVencimento = conta.DtVencimento;
        registro.DtPagamento = conta.DtPagamento;
        registro.VlValor = conta.VlValor;
        registro.VlPago = conta.VlPago;
        registro.DsStatus = status;
        registro.DsObservacao = observacao;

        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return registro;
    }

    // Baixa financeira: marca como pago hoje e, sem valor pago informado, usa o valor do titulo.
    public async Task Baixar(int id)
    {
        // Linha travada ate gravar: cancelamento da compra/venda ao mesmo tempo espera.
        await using var tx = await _db.Database.BeginTransactionAsync();
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM dbo.tbFinContasAPagar WITH (UPDLOCK, ROWLOCK) WHERE idContaPagar = {id}");
        var conta = await _db.ContasAPagar.FindAsync(id)
            ?? throw new ValidacaoException("Conta a pagar não encontrada.");
        if (conta.DsStatus == ContaAPagar.CANCELADO || (conta.OriginadoDeCompra && await CompraCancelada(conta)))
            throw new ValidacaoException("Conta cancelada não pode receber baixa.");
        if (conta.DsStatus == ContaAPagar.PAGO)
            throw new ValidacaoException("Esta conta já está paga.");

        conta.DsStatus = ContaAPagar.PAGO;
        conta.DtPagamento = DateOnly.FromDateTime(DateTime.Today);
        if (conta.VlPago == 0) conta.VlPago = conta.VlValor;
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    // Titulo gerado por uma compra que ja foi cancelada.
    private Task<bool> CompraCancelada(ContaAPagar c) =>
        _db.Compras.AnyAsync(x => x.NrNota == c.NrNota && x.NrSerie == c.NrSerie && x.NrModelo == c.NrModelo
                               && x.IdFornecedor == c.IdFornecedor && x.TpStatus == Compra.CANCELADA);

    // Codigos dos titulos (da lista) gerados por compra ja cancelada: na tela
    // eles abrem com todos os campos travados.
    public async Task<HashSet<int>> IdsDeCompraCancelada(IEnumerable<ContaAPagar> contas)
    {
        var ids = contas.Where(c => c.OriginadoDeCompra).Select(c => c.IdContaPagar).ToList();
        if (ids.Count == 0) return [];

        var cancelados = await _db.ContasAPagar
            .Where(c => ids.Contains(c.IdContaPagar)
                     && _db.Compras.Any(x => x.NrNota == c.NrNota && x.NrSerie == c.NrSerie && x.NrModelo == c.NrModelo
                                          && x.IdFornecedor == c.IdFornecedor && x.TpStatus == Compra.CANCELADA))
            .Select(c => c.IdContaPagar)
            .ToListAsync();
        return cancelados.ToHashSet();
    }

    public async Task Excluir(int id)
    {
        var conta = await _db.ContasAPagar.FindAsync(id);
        if (conta == null)
            return;

        // Titulo da compra nao e excluido (fica o historico). Compra ja cancelada:
        // a mensagem diz que o titulo ja esta cancelado, nao manda cancelar de novo.
        if (conta.OriginadoDeCompra && await CompraCancelada(conta))
            throw new ValidacaoException($"Este título é da compra Nº {conta.NrNota}, que foi cancelada.",
                                         "O título já está cancelado e não pode ser excluído: ele fica como histórico da compra.");
        if (conta.OriginadoDeCompra)
            throw new ValidacaoException($"Este título foi gerado pela compra Nº {conta.NrNota}.",
                                         "Para retirá-lo, altere ou cancele a compra.");

        // Nenhuma tabela referencia tbFinContasAPagar hoje; o catch fica como
        // rede de seguranca caso alguma FK nova aponte para ca.
        try
        {
            _db.ContasAPagar.Remove(conta);
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new ValidacaoException("Não é possível excluir: existem registros vinculados a esta conta.");
        }
    }
}
