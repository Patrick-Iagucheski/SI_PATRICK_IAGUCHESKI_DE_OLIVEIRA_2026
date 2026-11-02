using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;

namespace WebAppERP.Business.Financeiro;

// Regras de negocio da condicao de pagamento (com as parcelas). Usada pela
// tela de Condicoes de Pagamento, pelo componente de cadastro de condicao
// (inclusive o "+" no fornecedor e na Compra) e pelas listas de condicoes.
public class CondicaoPagamentoBusiness
{
    private readonly AppDbContext _db;

    public CondicaoPagamentoBusiness(AppDbContext db) => _db = db;

    // ============================================================
    // Consultas
    // ============================================================

    // Com as parcelas e a forma de cada uma (tela). Listas para escolher: so ativas.
    public async Task<List<CondicaoPagamento>> Listar(bool somenteAtivas = false) =>
        await _db.CondicoesPagamento
            .Include(c => c.Parcelas).ThenInclude(p => p.FormaPagamento)
            .Where(c => !somenteAtivas || c.FlAtivo)
            .OrderBy(c => c.DsCondicao)
            .ToListAsync();

    // Pesquisa da tela: por codigo ou descricao. Sem "inativas" marcado traz so
    // as ativas; marcado, so as inativas.
    public async Task<List<CondicaoPagamento>> Buscar(int? id, string? condicao, bool somenteInativas)
    {
        var query = _db.CondicoesPagamento
            .Include(c => c.Parcelas).ThenInclude(p => p.FormaPagamento)
            .Where(c => c.FlAtivo != somenteInativas);

        if (id.HasValue)
            query = query.Where(c => c.IdCondicaoPagamento == id.Value);

        if (!string.IsNullOrWhiteSpace(condicao))
        {
            query = query.Where(Module.MontarFiltroTexto<CondicaoPagamento>(condicao, c => c.DsCondicao));
        }

        return await query.OrderBy(c => c.DsCondicao).Take(100).ToListAsync();
    }

    // Parcelas da condicao (dias, percentual e forma): "Gerar parcelas" da Compra.
    public async Task<List<CondicaoPagamentoParcela>> GetParcelas(int idCondicao) =>
        await _db.CondicoesPagamentoParcelas
            .Where(p => p.IdCondicaoPagamento == idCondicao)
            .OrderBy(p => p.NrParcela)
            .ToListAsync();

    // ============================================================
    // Gravacao
    // ============================================================

    // Inclui (IdCondicaoPagamento = 0) ou altera a condicao com as parcelas
    // (substituidas por completo) e devolve o registro gravado.
    public async Task<CondicaoPagamento> Salvar(CondicaoPagamento condicao, List<CondicaoPagamentoParcela> parcelas)
    {
        var nome = condicao.DsCondicao?.Trim() ?? string.Empty;

        if (nome.Length == 0)
            throw new ValidacaoException("Informe a condição de pagamento.");
        if (nome.Length > 100)
            throw new ValidacaoException("A condição de pagamento deve ter no máximo 100 caracteres.");
        if (condicao.VlJuro < 0 || condicao.VlMulta < 0 || condicao.VlDesconto < 0)
            throw new ValidacaoException("Juro, multa e desconto não podem ser negativos.");

        if (parcelas.Count == 0)
            throw new ValidacaoException("Adicione ao menos uma parcela.");
        if (parcelas.Any(p => p.IdFormaPagamento <= 0))
            throw new ValidacaoException("Selecione a forma de pagamento em todas as parcelas.");
        if (parcelas.Any(p => p.VlPercentual <= 0 || p.VlPercentual > 100))
            throw new ValidacaoException("O percentual de cada parcela deve estar entre 0 e 100.");
        if (parcelas.Any(p => p.NrDiasVencimento < 0))
            throw new ValidacaoException("Os dias para vencimento não podem ser negativos.");

        var soma = parcelas.Sum(p => p.VlPercentual);
        if (Math.Abs(soma - 100m) > 0.01m)
            throw new ValidacaoException($"O percentual total das parcelas deve ser 100%. Atual: {soma:N2}%.");

        // Mesmo nome (sem diferenciar maiusculas nem acentos)
        var repetida = await _db.CondicoesPagamento
            .Where(c => c.IdCondicaoPagamento != condicao.IdCondicaoPagamento
                     && EF.Functions.Collate(c.DsCondicao, Module.CollationSemAcento) == nome)
            .Select(c => c.DsCondicao)
            .FirstOrDefaultAsync();
        if (repetida != null)
            throw new ValidacaoException($"Já existe a condição de pagamento \"{repetida}\".");

        // Renumera as parcelas (1..N) pela ordem de vencimento.
        var ordenadas = parcelas
            .OrderBy(p => p.NrDiasVencimento)
            .Select((p, i) => new CondicaoPagamentoParcela
            {
                NrParcela = i + 1,
                NrDiasVencimento = p.NrDiasVencimento,
                IdFormaPagamento = p.IdFormaPagamento,
                VlPercentual = Math.Round(p.VlPercentual, 2)
            })
            .ToList();

        CondicaoPagamento registro;
        if (condicao.IdCondicaoPagamento == 0)
        {
            registro = new CondicaoPagamento { DtCriacao = DateTime.Now };
            _db.CondicoesPagamento.Add(registro);
        }
        else
        {
            registro = await _db.CondicoesPagamento
                .Include(c => c.Parcelas)
                .FirstOrDefaultAsync(c => c.IdCondicaoPagamento == condicao.IdCondicaoPagamento)
                ?? throw new ValidacaoException("Condição de pagamento não encontrada.", "Ela pode ter sido excluída por outra pessoa.");
            registro.DtEdicao = DateTime.Now;
            _db.CondicoesPagamentoParcelas.RemoveRange(registro.Parcelas);
        }

        registro.DsCondicao = nome;
        registro.VlJuro = Math.Round(condicao.VlJuro, 2);
        registro.VlMulta = Math.Round(condicao.VlMulta, 2);
        registro.VlDesconto = Math.Round(condicao.VlDesconto, 2);
        registro.FlAtivo = condicao.FlAtivo;
        foreach (var p in ordenadas)
            registro.Parcelas.Add(p);

        await _db.SaveChangesAsync();
        return registro;
    }

    // Ativa/inativa e devolve a mensagem de sucesso.
    public async Task<string> AlterarSituacao(int id)
    {
        var condicao = await _db.CondicoesPagamento.FindAsync(id)
            ?? throw new ValidacaoException("Condição de pagamento não encontrada.", "Ela pode ter sido excluída por outra pessoa.");

        condicao.FlAtivo = !condicao.FlAtivo;
        condicao.DtEdicao = DateTime.Now;
        await _db.SaveChangesAsync();
        return condicao.FlAtivo ? "Condição de pagamento ativada." : "Condição de pagamento inativada.";
    }

    public async Task Excluir(int id)
    {
        var condicao = await _db.CondicoesPagamento.FindAsync(id);
        if (condicao == null)
            return;

        const string dica = "Inative a condição de pagamento em vez de excluí-la.";

        // Filhas: fornecedores (condicao padrao), compras e parcelas de compra.
        // As parcelas da propria condicao saem por cascade.
        if (await _db.Fornecedores.AnyAsync(f => f.IdCondicaoPagamento == id))
            throw new ValidacaoException("Não é possível excluir: esta condição é a padrão de um fornecedor.", dica);
        if (await _db.Compras.AnyAsync(c => c.IdCondicaoPagamento == id)
            || await _db.CompraPagamentos.AnyAsync(p => p.IdCondicaoPagamento == id))
            throw new ValidacaoException("Não é possível excluir: existe compra com esta condição de pagamento.", dica);

        try
        {
            _db.CondicoesPagamento.Remove(condicao);
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new ValidacaoException("Não é possível excluir: existem registros vinculados a esta condição de pagamento.", dica);
        }
    }
}
