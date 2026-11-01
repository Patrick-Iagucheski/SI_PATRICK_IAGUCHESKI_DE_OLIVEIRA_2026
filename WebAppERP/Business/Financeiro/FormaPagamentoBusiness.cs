using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;

namespace WebAppERP.Business.Financeiro;

// Regras de negocio da forma de pagamento. Usada pela tela de Formas de
// Pagamento e pelas listas de formas das outras telas (Contas a Pagar e o
// cadastro de condicao de pagamento).
public class FormaPagamentoBusiness
{
    private readonly AppDbContext _db;

    public FormaPagamentoBusiness(AppDbContext db) => _db = db;

    // ============================================================
    // Consultas
    // ============================================================

    // A tela lista todas; as listas para escolher, so as ativas.
    public async Task<List<FormaPagamento>> Listar(bool somenteAtivas = false) =>
        await _db.FormasPagamento
            .Where(f => !somenteAtivas || f.FlAtivo)
            .OrderBy(f => f.DsFormaPagamento)
            .ToListAsync();

    // ============================================================
    // Gravacao
    // ============================================================

    // Inclui (IdFormaPagamento = 0) ou altera a forma e devolve o registro gravado.
    public async Task<FormaPagamento> Salvar(FormaPagamento forma)
    {
        var nome = forma.DsFormaPagamento?.Trim() ?? string.Empty;

        if (nome.Length == 0)
            throw new ValidacaoException("Informe a forma de pagamento.");
        if (nome.Length > 100)
            throw new ValidacaoException("A forma de pagamento deve ter no máximo 100 caracteres.");

        // Mesmo nome (sem diferenciar maiusculas nem acentos)
        var repetida = await _db.FormasPagamento
            .Where(f => f.IdFormaPagamento != forma.IdFormaPagamento
                     && EF.Functions.Collate(f.DsFormaPagamento, Module.CollationSemAcento) == nome)
            .Select(f => f.DsFormaPagamento)
            .FirstOrDefaultAsync();
        if (repetida != null)
            throw new ValidacaoException($"Já existe a forma de pagamento \"{repetida}\".");

        FormaPagamento registro;
        if (forma.IdFormaPagamento == 0)
        {
            registro = new FormaPagamento { DtCriacao = DateTime.Now };
            _db.FormasPagamento.Add(registro);
        }
        else
        {
            registro = await _db.FormasPagamento.FindAsync(forma.IdFormaPagamento)
                ?? throw new ValidacaoException("Forma de pagamento não encontrada.", "Ela pode ter sido excluída por outra pessoa.");
        }

        registro.DsFormaPagamento = nome;
        registro.FlAtivo = forma.FlAtivo;

        await _db.SaveChangesAsync();
        return registro;
    }

    // Ativa/inativa e devolve a nova situacao (null se nao existe).
    public async Task<bool?> AlterarSituacao(int id)
    {
        var forma = await _db.FormasPagamento.FindAsync(id);
        if (forma == null)
            return null;

        forma.FlAtivo = !forma.FlAtivo;
        await _db.SaveChangesAsync();
        return forma.FlAtivo;
    }

    public async Task Excluir(int id)
    {
        var forma = await _db.FormasPagamento.FindAsync(id);
        if (forma == null)
            return;

        const string dica = "Inative a forma de pagamento em vez de excluí-la.";

        // Filhas: parcelas de condicao, contas a pagar, parcelas de compra e vendas.
        if (await _db.CondicoesPagamentoParcelas.AnyAsync(p => p.IdFormaPagamento == id))
            throw new ValidacaoException("Não é possível excluir: esta forma é usada em uma condição de pagamento.", dica);
        if (await _db.ContasAPagar.AnyAsync(c => c.IdFormaPagamento == id))
            throw new ValidacaoException("Não é possível excluir: existe conta a pagar com esta forma de pagamento.", dica);
        if (await _db.CompraPagamentos.AnyAsync(p => p.IdFormaPagamento == id))
            throw new ValidacaoException("Não é possível excluir: existe compra paga com esta forma de pagamento.", dica);
        if (await _db.Vendas.AnyAsync(v => v.IdFormaPagamento == id))
            throw new ValidacaoException("Não é possível excluir: existe venda com esta forma de pagamento.", dica);

        try
        {
            _db.FormasPagamento.Remove(forma);
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new ValidacaoException("Não é possível excluir: existem registros vinculados a esta forma de pagamento.", dica);
        }
    }
}
