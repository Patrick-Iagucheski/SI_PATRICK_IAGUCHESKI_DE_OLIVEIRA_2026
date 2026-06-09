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

    // Classifica a situacao da parcela na data atual.
    public static string Situacao(FinContaPagar c, DateOnly hoje)
    {
        if (c.DtPagamento != null) return "PAGA";
        if (c.DtVencimento != null && c.DtVencimento < hoje) return "VENCIDA";
        return "ABERTA";
    }

    public async Task OnGetAsync()
    {
        Contas = await _db.ContasAPagar
            .Include(c => c.Fornecedor)
            .Include(c => c.FormaPagamento)
            .OrderBy(c => c.DtVencimento)
            .ToListAsync();

        Fornecedores = await _db.Fornecedores.OrderBy(f => f.DsRazaoSocial).ToListAsync();
        Formas = await _db.FormasPagamento.Where(f => f.FlAtivo).OrderBy(f => f.DsFormaPagamento).ToListAsync();

        var hoje = DateOnly.FromDateTime(DateTime.Today);
        foreach (var c in Contas)
        {
            var valor = c.VlParcela ?? 0;
            switch (Situacao(c, hoje))
            {
                case "PAGA": QtPagas++; VlPagas += valor; break;
                case "VENCIDA": QtVencidas++; VlVencidas += valor; break;
                default: QtAberto++; VlAberto += valor; break;
            }
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var existing = await _db.ContasAPagar.FindAsync(
            ContaForm.NrNFe, ContaForm.NrSerie, ContaForm.NrModelo, ContaForm.IdFornecedor, ContaForm.NrParcela);

        if (existing == null)
        {
            _db.ContasAPagar.Add(ContaForm);
        }
        else
        {
            // A chave (NFe/Serie/Modelo/Fornecedor/Parcela) identifica a linha e nao e alterada na edicao.
            existing.VlParcela = ContaForm.VlParcela;
            existing.DtVencimento = ContaForm.DtVencimento;
            existing.DtPagamento = ContaForm.DtPagamento;
            existing.IdFormaPagamento = ContaForm.IdFormaPagamento;
            existing.DsObservacao = ContaForm.DsObservacao;
        }

        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    // Baixa financeira: marca a parcela como paga na data de hoje.
    public async Task<IActionResult> OnPostBaixaAsync(int nrNFe, int nrSerie, int nrModelo, int idFornecedor, int nrParcela)
    {
        var conta = await _db.ContasAPagar.FindAsync(nrNFe, nrSerie, nrModelo, idFornecedor, nrParcela);
        if (conta != null)
        {
            conta.DtPagamento = DateOnly.FromDateTime(DateTime.Today);
            await _db.SaveChangesAsync();
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int nrNFe, int nrSerie, int nrModelo, int idFornecedor, int nrParcela)
    {
        var conta = await _db.ContasAPagar.FindAsync(nrNFe, nrSerie, nrModelo, idFornecedor, nrParcela);
        if (conta != null)
        {
            _db.ContasAPagar.Remove(conta);
            await _db.SaveChangesAsync();
        }
        return RedirectToPage();
    }
}
