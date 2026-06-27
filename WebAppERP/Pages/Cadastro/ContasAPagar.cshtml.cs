using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;
using WebAppERP.Models;

namespace WebAppERP.Pages.Cadastro;

public class ContasAPagarModel : PageModel
{
    private readonly AppDbContext _db;

    public ContasAPagarModel(AppDbContext db) => _db = db;

    public List<FinContaPagar> Contas { get; set; } = [];
    public List<GerFornecedor> Fornecedores { get; set; } = [];
    public List<FinFormaPagamento> Formas { get; set; } = [];

    // Indicadores
    public int QtAberto { get; set; }
    public int QtVencidas { get; set; }
    public int QtPagas { get; set; }
    public decimal VlAberto { get; set; }
    public decimal VlVencidas { get; set; }
    public decimal VlPagas { get; set; }

    [BindProperty]
    public FinContaPagar ContaForm { get; set; } = new();

    // Situacao apresentada na tela: PAGO / CANCELADO / VENCIDA / PENDENTE.
    public static string Situacao(FinContaPagar c, DateOnly hoje)
    {
        if (c.DsStatus == "PAGO") return "PAGO";
        if (c.DsStatus == "CANCELADO") return "CANCELADO";
        if (c.DtVencimento != null && c.DtVencimento < hoje) return "VENCIDA";
        return "PENDENTE";
    }

    public async Task OnGetAsync()
    {
        Contas = await _db.ContasAPagar
            .Include(c => c.Fornecedor)
            .Include(c => c.FormaPagamento)
            .OrderBy(c => c.DtVencimento)
            .ThenBy(c => c.IdContaPagar)
            .ToListAsync();

        Fornecedores = await _db.Fornecedores.OrderBy(f => f.DsRazaoSocial).ToListAsync();
        Formas = await _db.FormasPagamento.Where(f => f.FlAtivo).OrderBy(f => f.DsFormaPagamento).ToListAsync();

        var hoje = DateOnly.FromDateTime(DateTime.Today);
        foreach (var c in Contas)
        {
            switch (Situacao(c, hoje))
            {
                case "PAGO": QtPagas++; VlPagas += c.VlPago; break;
                case "VENCIDA": QtVencidas++; VlVencidas += c.VlValor; break;
                case "PENDENTE": QtAberto++; VlAberto += c.VlValor; break;
            }
        }
    }

    private static readonly string[] StatusValidos = { "PENDENTE", "PAGO", "CANCELADO" };

    private static string? Truncar(string? valor, int max) =>
        string.IsNullOrEmpty(valor) ? valor : (valor.Length > max ? valor[..max] : valor);

    public async Task<IActionResult> OnPostAsync()
    {
        var status = (ContaForm.DsStatus ?? "PENDENTE").Trim().ToUpper();
        if (!StatusValidos.Contains(status)) status = "PENDENTE";
        ContaForm.DsStatus = status;

        ContaForm.DsConta = Truncar(ContaForm.DsConta?.Trim(), 100) ?? string.Empty;
        ContaForm.NrDocumento = Truncar(ContaForm.NrDocumento?.Trim(), 20);
        ContaForm.DsObservacao = Truncar(ContaForm.DsObservacao, 500);

        if (string.IsNullOrWhiteSpace(ContaForm.DsConta))
            return RedirectToPage();
        if (ContaForm.IdFornecedor <= 0)
            return RedirectToPage();

        if (ContaForm.IdContaPagar == 0)
        {
            ContaForm.DtCriacao = DateTime.Now;
            _db.ContasAPagar.Add(ContaForm);
        }
        else
        {
            var existing = await _db.ContasAPagar.FindAsync(ContaForm.IdContaPagar);
            if (existing != null)
            {
                existing.DsConta = ContaForm.DsConta;
                existing.NrDocumento = ContaForm.NrDocumento;
                existing.IdFornecedor = ContaForm.IdFornecedor;
                existing.IdFormaPagamento = ContaForm.IdFormaPagamento;
                existing.DtEmissao = ContaForm.DtEmissao;
                existing.DtVencimento = ContaForm.DtVencimento;
                existing.DtPagamento = ContaForm.DtPagamento;
                existing.VlValor = ContaForm.VlValor;
                existing.VlPago = ContaForm.VlPago;
                existing.DsStatus = ContaForm.DsStatus;
                existing.FlAtivo = ContaForm.FlAtivo;
                existing.DsObservacao = ContaForm.DsObservacao;
                existing.DtEdicao = DateTime.Now;
            }
        }

        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    // Baixa financeira: marca como pago na data de hoje e replica o valor.
    public async Task<IActionResult> OnPostBaixaAsync(int id)
    {
        var conta = await _db.ContasAPagar.FindAsync(id);
        if (conta != null)
        {
            conta.DsStatus = "PAGO";
            conta.DtPagamento = DateOnly.FromDateTime(DateTime.Today);
            if (conta.VlPago == 0) conta.VlPago = conta.VlValor;
            conta.DtEdicao = DateTime.Now;
            await _db.SaveChangesAsync();
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var conta = await _db.ContasAPagar.FindAsync(id);
        if (conta != null)
        {
            _db.ContasAPagar.Remove(conta);
            await _db.SaveChangesAsync();
        }
        return RedirectToPage();
    }

    // ============================================================
    // Busca de contas a pagar via AJAX (filtros da pagina)
    // Pesquisa por: codigo, conta (descricao), documento ou fornecedor
    // ============================================================
    public async Task<IActionResult> OnGetBuscarContasAsync(
        int? id, string? conta, string? documento, int? idFornecedor, bool incluirInativos = false)
    {
        var query = _db.ContasAPagar
            .Include(c => c.Fornecedor)
            .Include(c => c.FormaPagamento)
            .AsQueryable();

        // Checkbox desmarcada => apenas ativas. Marcada => apenas inativas.
        if (incluirInativos)
            query = query.Where(c => !c.FlAtivo);
        else
            query = query.Where(c => c.FlAtivo);

        // Todos os filtros sao opcionais e combinaveis.
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

        var lista = await query
            .OrderBy(c => c.DtVencimento)
            .ThenBy(c => c.IdContaPagar)
            .Take(100)
            .Select(c => new
            {
                id = c.IdContaPagar,
                conta = c.DsConta,
                documento = c.NrDocumento,
                idFornecedor = c.IdFornecedor,
                fornecedorNome = c.Fornecedor != null ? c.Fornecedor.DsRazaoSocial : null,
                idForma = c.IdFormaPagamento,
                formaNome = c.FormaPagamento != null ? c.FormaPagamento.DsFormaPagamento : null,
                emissao = c.DtEmissao,
                venc = c.DtVencimento,
                pgto = c.DtPagamento,
                valor = c.VlValor,
                pago = c.VlPago,
                status = c.DsStatus,
                ativo = c.FlAtivo,
                obs = c.DsObservacao
            })
            .ToListAsync();

        return new JsonResult(lista);
    }
}
