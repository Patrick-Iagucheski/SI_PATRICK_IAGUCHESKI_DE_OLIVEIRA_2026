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

    // Pesquisa da tela: por codigo, descricao, documento ou fornecedor. Sem
    // "inativas" marcado traz so as ativas; marcado, so as inativas.
    public async Task<List<ContaAPagar>> Buscar(int? id, string? conta, string? documento, int? idFornecedor, bool somenteInativas)
    {
        var query = _db.ContasAPagar
            .Include(c => c.Fornecedor)
            .Include(c => c.FormaPagamento)
            .Where(c => c.FlAtivo != somenteInativas);

        if (id.HasValue)
            query = query.Where(c => c.IdContaPagar == id.Value);

        if (!string.IsNullOrWhiteSpace(conta))
        {
            var n = conta.Trim();
            query = query.Where(c => c.DsConta.Contains(n));
        }

        if (!string.IsNullOrWhiteSpace(documento))
        {
            var d = documento.Trim();
            query = query.Where(c => c.NrDocumento != null && c.NrDocumento.Contains(d));
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
        if (conta.DtEmissao != null && conta.DtVencimento != null && conta.DtVencimento < conta.DtEmissao)
            throw new ValidacaoException("O vencimento não pode ser anterior à emissão.");

        ContaAPagar registro;
        if (conta.IdContaPagar == 0)
        {
            registro = new ContaAPagar { DtCriacao = DateTime.Now };
            _db.ContasAPagar.Add(registro);
        }
        else
        {
            registro = await _db.ContasAPagar.FindAsync(conta.IdContaPagar)
                ?? throw new ValidacaoException("Conta a pagar não encontrada.", "Ela pode ter sido excluída por outra pessoa.");
            registro.DtEdicao = DateTime.Now;

            // Titulo da compra: so o pagamento (data, valor pago, situacao) e a
            // observacao mudam por aqui; o resto vem das parcelas da compra.
            if (registro.OriginadoDeCompra)
            {
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
        registro.FlAtivo = conta.FlAtivo;
        registro.DsObservacao = observacao;

        await _db.SaveChangesAsync();
        return registro;
    }

    // Baixa financeira: marca como pago hoje e, sem valor pago informado, usa o valor do titulo.
    public async Task Baixar(int id)
    {
        var conta = await _db.ContasAPagar.FindAsync(id)
            ?? throw new ValidacaoException("Conta a pagar não encontrada.");
        if (conta.DsStatus == ContaAPagar.CANCELADO)
            throw new ValidacaoException("Conta cancelada não pode receber baixa.");
        if (conta.DsStatus == ContaAPagar.PAGO)
            throw new ValidacaoException("Esta conta já está paga.");

        conta.DsStatus = ContaAPagar.PAGO;
        conta.DtPagamento = DateOnly.FromDateTime(DateTime.Today);
        if (conta.VlPago == 0) conta.VlPago = conta.VlValor;
        conta.DtEdicao = DateTime.Now;
        await _db.SaveChangesAsync();
    }

    public async Task Excluir(int id)
    {
        var conta = await _db.ContasAPagar.FindAsync(id);
        if (conta == null)
            return;

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
