using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;

namespace WebAppERP.Business.Financeiro;

// Regras de negocio do titulo a receber: tela de Contas a Receber e titulos
// gerados pela venda. Titulo da venda pode receber baixa, mas valor, cliente,
// datas e exclusao so mudam pela propria venda.
public class ContaAReceberBusiness
{
    private readonly AppDbContext _db;

    public ContaAReceberBusiness(AppDbContext db) => _db = db;

    private static readonly string[] StatusValidos = [ContaAReceber.PENDENTE, ContaAReceber.RECEBIDO, ContaAReceber.CANCELADO];

    // Situacao apresentada na tela: RECEBIDO / CANCELADO / VENCIDA / PENDENTE.
    public static string GetSituacao(ContaAReceber c, DateOnly hoje)
    {
        if (c.DsStatus == ContaAReceber.RECEBIDO) return ContaAReceber.RECEBIDO;
        if (c.DsStatus == ContaAReceber.CANCELADO) return ContaAReceber.CANCELADO;
        if (c.DtVencimento != null && c.DtVencimento < hoje) return ContaAReceber.VENCIDA;
        return ContaAReceber.PENDENTE;
    }

    // ============================================================
    // Consultas
    // ============================================================

    public async Task<List<ContaAReceber>> Listar() =>
        await _db.ContasAReceber
            .Include(c => c.Cliente)
            .Include(c => c.FormaPagamento)
            .OrderBy(c => c.DtVencimento)
            .ThenBy(c => c.IdContaReceber)
            .ToListAsync();

    // Pesquisa da tela: por codigo, descricao, documento, cliente e situacao.
    //   situacao: PENDENTE (inclui as vencidas, padrao da tela), VENCIDA,
    //             RECEBIDO, CANCELADO ou vazio (todas).
    public async Task<List<ContaAReceber>> Buscar(int? id, string? conta, string? documento, int? idCliente, string? situacao)
    {
        var query = _db.ContasAReceber
            .Include(c => c.Cliente)
            .Include(c => c.FormaPagamento)
            .AsQueryable();

        var hoje = DateOnly.FromDateTime(DateTime.Today);
        query = (situacao ?? "").Trim().ToUpper() switch
        {
            ContaAReceber.PENDENTE => query.Where(c => c.DsStatus == ContaAReceber.PENDENTE),
            ContaAReceber.VENCIDA => query.Where(c => c.DsStatus == ContaAReceber.PENDENTE && c.DtVencimento < hoje),
            ContaAReceber.RECEBIDO => query.Where(c => c.DsStatus == ContaAReceber.RECEBIDO),
            ContaAReceber.CANCELADO => query.Where(c => c.DsStatus == ContaAReceber.CANCELADO),
            _ => query
        };

        if (id.HasValue)
            query = query.Where(c => c.IdContaReceber == id.Value);

        if (!string.IsNullOrWhiteSpace(conta))
        {
            query = query.Where(Module.MontarFiltroTexto<ContaAReceber>(conta, c => c.DsConta));
        }

        if (!string.IsNullOrWhiteSpace(documento))
        {
            query = query.Where(Module.MontarFiltroTexto<ContaAReceber>(documento, c => c.NrDocumento));
        }

        if (idCliente.HasValue)
            query = query.Where(c => c.IdCliente == idCliente.Value);

        return await query.OrderBy(c => c.DtVencimento).ThenBy(c => c.IdContaReceber).Take(100).ToListAsync();
    }

    // Codigos das vendas canceladas entre as dos titulos da lista: esses titulos
    // abrem com todos os campos travados.
    public async Task<HashSet<int>> VendasCanceladas(IEnumerable<ContaAReceber> contas)
    {
        var vendas = contas.Where(c => c.IdVenda != null).Select(c => c.IdVenda!.Value).Distinct().ToList();
        if (vendas.Count == 0) return [];

        var canceladas = await _db.Vendas
            .Where(v => vendas.Contains(v.IdVenda) && v.TpStatus == Venda.CANCELADA)
            .Select(v => v.IdVenda)
            .ToListAsync();
        return canceladas.ToHashSet();
    }

    private async Task<bool> VendaCancelada(ContaAReceber c) =>
        (await VendasCanceladas([c])).Count > 0;

    // ============================================================
    // Gravacao
    // ============================================================

    // Inclui (IdContaReceber = 0) ou altera o titulo e devolve o registro gravado.
    public async Task<ContaAReceber> Salvar(ContaAReceber conta)
    {
        var status = (conta.DsStatus ?? ContaAReceber.PENDENTE).Trim().ToUpper();
        if (!StatusValidos.Contains(status)) status = ContaAReceber.PENDENTE;

        var descricao = Module.Truncar(conta.DsConta?.Trim(), 100) ?? string.Empty;
        var documento = string.IsNullOrWhiteSpace(conta.NrDocumento) ? null : Module.Truncar(conta.NrDocumento.Trim(), 20);
        var observacao = Module.Truncar(conta.DsObservacao, 500);

        if (descricao.Length == 0)
            throw new ValidacaoException("Informe a descrição da conta.");
        if (conta.IdFormaPagamento is int idForma && !await _db.FormasPagamento.AnyAsync(f => f.IdFormaPagamento == idForma))
            throw new ValidacaoException("Forma de pagamento inválida.");
        if (conta.VlValor <= 0)
            throw new ValidacaoException("Informe o valor da conta.");
        if (conta.VlRecebido < 0)
            throw new ValidacaoException("O valor recebido não pode ser negativo.");
        if (conta.DtEmissao != null && conta.DtVencimento != null && conta.DtVencimento < conta.DtEmissao)
            throw new ValidacaoException("O vencimento não pode ser anterior à emissão.");

        ContaAReceber registro;
        if (conta.IdContaReceber == 0)
        {
            await ValidarCliente(conta.IdCliente);
            registro = new ContaAReceber();
            _db.ContasAReceber.Add(registro);
        }
        else
        {
            registro = await _db.ContasAReceber.FindAsync(conta.IdContaReceber)
                ?? throw new ValidacaoException("Conta a receber não encontrada.", "Ela pode ter sido excluída por outra pessoa.");

            if (registro.OriginadoDeVenda)
                await ValidarAlteracaoDaVenda(registro, conta, status, descricao, documento);
            else
                await ValidarCliente(conta.IdCliente);
        }

        registro.DsConta = descricao;
        registro.NrDocumento = documento;
        registro.IdCliente = conta.IdCliente;
        registro.IdFormaPagamento = conta.IdFormaPagamento;
        registro.DtEmissao = conta.DtEmissao;
        registro.DtVencimento = conta.DtVencimento;
        registro.DtRecebimento = conta.DtRecebimento;
        registro.VlValor = conta.VlValor;
        registro.VlRecebido = conta.VlRecebido;
        registro.DsStatus = status;
        registro.DsObservacao = observacao;

        await _db.SaveChangesAsync();
        return registro;
    }

    private async Task ValidarCliente(int? idCliente)
    {
        if (idCliente is not > 0 || !await _db.Clientes.AnyAsync(c => c.IdCliente == idCliente))
            throw new ValidacaoException("Selecione o cliente da conta.");
    }

    // Titulo da venda: so o recebimento (data, valor, situacao), a forma e a
    // observacao mudam por aqui; o resto vem da venda.
    private async Task ValidarAlteracaoDaVenda(ContaAReceber registro, ContaAReceber conta, string status, string descricao, string? documento)
    {
        var titulo = $"Este título foi gerado pela venda Nº {registro.IdVenda}.";

        if (await VendaCancelada(registro))
            throw new ValidacaoException($"Este título é da venda Nº {registro.IdVenda}, que foi cancelada.",
                                         "Título de venda cancelada não pode ser alterado.");
        if (registro.RecebidoNaVenda)
            throw new ValidacaoException(titulo, "Título recebido no ato da venda não pode ser alterado.");
        if (registro.IdCliente != conta.IdCliente || registro.VlValor != conta.VlValor
            || registro.DtEmissao != conta.DtEmissao || registro.DtVencimento != conta.DtVencimento
            || registro.DsConta != descricao || registro.NrDocumento != documento)
            throw new ValidacaoException(titulo, "Cliente, valor, datas e descrição só podem ser alterados pela própria venda.");
        if (status == ContaAReceber.CANCELADO && registro.DsStatus != ContaAReceber.CANCELADO)
            throw new ValidacaoException(titulo, "Para cancelá-lo, cancele a venda.");
    }

    // Baixa: marca como recebido hoje e, sem valor informado, usa o valor do titulo.
    public async Task Baixar(int id)
    {
        var conta = await _db.ContasAReceber.FindAsync(id)
            ?? throw new ValidacaoException("Conta a receber não encontrada.");
        if (conta.DsStatus == ContaAReceber.CANCELADO || (conta.OriginadoDeVenda && await VendaCancelada(conta)))
            throw new ValidacaoException("Conta cancelada não pode receber baixa.");
        if (conta.DsStatus == ContaAReceber.RECEBIDO)
            throw new ValidacaoException("Esta conta já está recebida.");

        conta.DsStatus = ContaAReceber.RECEBIDO;
        conta.DtRecebimento = DateOnly.FromDateTime(DateTime.Today);
        if (conta.VlRecebido == 0) conta.VlRecebido = conta.VlValor;
        await _db.SaveChangesAsync();
    }

    public async Task Excluir(int id)
    {
        var conta = await _db.ContasAReceber.FindAsync(id);
        if (conta == null)
            return;

        // Titulo da venda nao e excluido (fica o historico).
        if (conta.OriginadoDeVenda && await VendaCancelada(conta))
            throw new ValidacaoException($"Este título é da venda Nº {conta.IdVenda}, que foi cancelada.",
                                         "O título já está cancelado e não pode ser excluído: ele fica como histórico da venda.");
        if (conta.OriginadoDeVenda)
            throw new ValidacaoException($"Este título foi gerado pela venda Nº {conta.IdVenda}.",
                                         "Para retirá-lo, altere ou cancele a venda.");

        _db.ContasAReceber.Remove(conta);
        await _db.SaveChangesAsync();
    }

    // ============================================================
    // Titulos da venda (chamados por VendaBusiness, na transacao dela)
    // ============================================================

    // Monta os titulos da venda finalizada. parcelas: as da condicao de
    // pagamento; vazia, a venda e a vista em uma parcela so. A ultima absorve o
    // arredondamento. Parcela sem prazo ja nasce recebida. O Business da venda grava.
    public void GerarDaVenda(Venda venda, IReadOnlyList<CondicaoPagamentoParcela> parcelas)
    {
        var emissao = DateOnly.FromDateTime(venda.DtVenda);
        var quantidade = Math.Max(parcelas.Count, 1);
        decimal acumulado = 0;

        for (var n = 1; n <= quantidade; n++)
        {
            var parcela = parcelas.Count > 0 ? parcelas[n - 1] : null;
            var valor = n == quantidade
                ? venda.VlTotal - acumulado
                : Math.Round(venda.VlTotal * parcela!.VlPercentual / 100, 2, MidpointRounding.AwayFromZero);
            acumulado += valor;

            var dias = parcela?.NrDiasVencimento ?? 0;
            var aVista = dias == 0;

            _db.ContasAReceber.Add(new ContaAReceber
            {
                DsConta = $"Venda Nº {venda.IdVenda} - Parcela {n}/{quantidade}",
                NrDocumento = venda.IdVenda.ToString(),
                IdCliente = venda.IdCliente,
                IdFormaPagamento = parcela?.IdFormaPagamento ?? venda.IdFormaPagamento,
                DtEmissao = emissao,
                DtVencimento = emissao.AddDays(dias),
                DtRecebimento = aVista ? emissao : null,
                VlValor = valor,
                VlRecebido = aVista ? valor : 0,
                DsStatus = aVista ? ContaAReceber.RECEBIDO : ContaAReceber.PENDENTE,
                DsObservacao = $"Gerado automaticamente pela venda Nº {venda.IdVenda}.",
                IdVenda = venda.IdVenda,
                NrParcela = n
            });
        }
    }

    private Task<List<ContaAReceber>> TitulosDaVenda(int idVenda) =>
        _db.ContasAReceber.Where(c => c.IdVenda == idVenda).ToListAsync();

    public Task<bool> VendaTemTitulos(int idVenda) =>
        _db.ContasAReceber.AnyAsync(c => c.IdVenda == idVenda);

    // Parcela a prazo ja recebida impede alterar ou cancelar a venda. A parcela
    // a vista nao conta: ela nasce recebida junto com a venda.
    private static void ValidarSemRecebimento(List<ContaAReceber> titulos, string acao)
    {
        if (titulos.Any(c => !c.RecebidoNaVenda && (c.DsStatus == ContaAReceber.RECEBIDO || c.VlRecebido > 0)))
            throw new ValidacaoException($"Não é possível {acao}: existe parcela já recebida no Contas a Receber.",
                                         "Estorne o recebimento primeiro.");
    }

    // Venda finalizada sendo alterada: os titulos sao gerados de novo.
    public async Task RemoverDaVenda(int idVenda)
    {
        var titulos = await TitulosDaVenda(idVenda);
        ValidarSemRecebimento(titulos, "alterar a venda");
        _db.ContasAReceber.RemoveRange(titulos);
    }

    // Titulos sao cancelados (nao excluidos) para preservar o historico.
    public async Task CancelarDaVenda(int idVenda)
    {
        var titulos = await TitulosDaVenda(idVenda);
        ValidarSemRecebimento(titulos, "cancelar a venda");
        foreach (var titulo in titulos)
            titulo.DsStatus = ContaAReceber.CANCELADO;
    }
}
