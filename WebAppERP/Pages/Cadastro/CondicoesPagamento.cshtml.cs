using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;
using WebAppERP.Models;

namespace WebAppERP.Pages.Cadastro;

public class CondicoesPagamentoModel : PageModel
{
    private readonly AppDbContext _db;

    public CondicoesPagamentoModel(AppDbContext db) => _db = db;

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public List<FinCondicaoPagamento> Condicoes { get; set; } = [];
    public List<FinFormaPagamento> Formas { get; set; } = [];

    [BindProperty] public FinCondicaoPagamento CondicaoForm { get; set; } = new();
    [BindProperty] public string ParcelasJson { get; set; } = "[]";

    // DTO recebido da tela (parcelas serializadas em JSON).
    public class ParcelaDto
    {
        public int NrParcela { get; set; }
        public int NrDiasVencimento { get; set; }
        public int IdFormaPagamento { get; set; }
        public decimal VlPercentual { get; set; }
    }

    public async Task OnGetAsync()
    {
        Condicoes = await _db.CondicoesPagamento
            .Include(c => c.Parcelas)
                .ThenInclude(p => p.FormaPagamento)
            .OrderBy(c => c.DsCondicao)
            .ToListAsync();

        Formas = await _db.FormasPagamento.Where(f => f.FlAtivo).OrderBy(f => f.DsFormaPagamento).ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var parcelas = JsonSerializer.Deserialize<List<ParcelaDto>>(ParcelasJson ?? "[]", JsonOpts) ?? [];

        // ===== Validacoes de servidor (defensivas; a tela ja valida) =====
        if (string.IsNullOrWhiteSpace(CondicaoForm.DsCondicao))
            return await ComErroAsync("Informe a condicao de pagamento.");

        if (parcelas.Count == 0)
            return await ComErroAsync("Adicione ao menos uma parcela.");

        if (parcelas.Any(p => p.IdFormaPagamento <= 0))
            return await ComErroAsync("Selecione a forma de pagamento em todas as parcelas.");

        if (parcelas.Any(p => p.VlPercentual <= 0 || p.VlPercentual > 100))
            return await ComErroAsync("O percentual de cada parcela deve estar entre 0 e 100.");

        if (parcelas.Any(p => p.NrDiasVencimento < 0))
            return await ComErroAsync("Os dias para vencimento nao podem ser negativos.");

        var soma = parcelas.Sum(p => p.VlPercentual);
        if (Math.Abs(soma - 100m) > 0.01m)
            return await ComErroAsync($"O percentual total das parcelas deve ser 100%. Atual: {soma:N2}%.");

        // Renumera as parcelas (1..N) garantindo a chave composta.
        var ordenadas = parcelas
            .OrderBy(p => p.NrDiasVencimento)
            .Select((p, i) => new FinCondicaoPagamentoParcela
            {
                NrParcela = i + 1,
                NrDiasVencimento = p.NrDiasVencimento,
                IdFormaPagamento = p.IdFormaPagamento,
                VlPercentual = Math.Round(p.VlPercentual, 2)
            })
            .ToList();

        var nome = CondicaoForm.DsCondicao.Trim();
        nome = nome.Length > 100 ? nome[..100] : nome;

        // Garante no maximo 2 casas decimais nos percentuais.
        CondicaoForm.VlJuro = Math.Round(CondicaoForm.VlJuro, 2);
        CondicaoForm.VlMulta = Math.Round(CondicaoForm.VlMulta, 2);
        CondicaoForm.VlDesconto = Math.Round(CondicaoForm.VlDesconto, 2);

        if (CondicaoForm.IdCondicaoPagamento == 0)
        {
            var nova = new FinCondicaoPagamento
            {
                DsCondicao = nome,
                VlJuro = CondicaoForm.VlJuro,
                VlMulta = CondicaoForm.VlMulta,
                VlDesconto = CondicaoForm.VlDesconto,
                FlAtivo = CondicaoForm.FlAtivo,
                DtCriacao = DateTime.Now,
                Parcelas = ordenadas
            };
            _db.CondicoesPagamento.Add(nova);
        }
        else
        {
            var existing = await _db.CondicoesPagamento
                .Include(c => c.Parcelas)
                .FirstOrDefaultAsync(c => c.IdCondicaoPagamento == CondicaoForm.IdCondicaoPagamento);

            if (existing != null)
            {
                existing.DsCondicao = nome;
                existing.VlJuro = CondicaoForm.VlJuro;
                existing.VlMulta = CondicaoForm.VlMulta;
                existing.VlDesconto = CondicaoForm.VlDesconto;
                existing.FlAtivo = CondicaoForm.FlAtivo;
                existing.DtEdicao = DateTime.Now;

                // Substitui o conjunto de parcelas.
                _db.CondicoesPagamentoParcelas.RemoveRange(existing.Parcelas);
                foreach (var p in ordenadas)
                {
                    p.IdCondicaoPagamento = existing.IdCondicaoPagamento;
                    existing.Parcelas.Add(p);
                }
            }
        }

        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var c = await _db.CondicoesPagamento.FindAsync(id);
        if (c != null)
        {
            c.FlAtivo = !c.FlAtivo;
            c.DtEdicao = DateTime.Now;
            await _db.SaveChangesAsync();
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var c = await _db.CondicoesPagamento.FindAsync(id);
        if (c != null)
        {
            // Parcelas saem por cascade (configurado no DbContext / FK).
            _db.CondicoesPagamento.Remove(c);
            await _db.SaveChangesAsync();
        }
        return RedirectToPage();
    }

    private async Task<IActionResult> ComErroAsync(string mensagem)
    {
        TempData["ErroCondicao"] = mensagem;
        await OnGetAsync();
        return Page();
    }

    // ============================================================
    // Busca de condicoes de pagamento via AJAX (filtros da pagina)
    // Pesquisa por: codigo ou condicao (descricao)
    // ============================================================
    public async Task<IActionResult> OnGetBuscarCondicoesAsync(
        int? id, string? condicao, bool incluirInativos = false)
    {
        var query = _db.CondicoesPagamento
            .Include(c => c.Parcelas)
                .ThenInclude(p => p.FormaPagamento)
            .AsQueryable();

        // Checkbox desmarcada => apenas ativas. Marcada => apenas inativas.
        if (incluirInativos)
            query = query.Where(c => !c.FlAtivo);
        else
            query = query.Where(c => c.FlAtivo);

        // Filtros opcionais e combinaveis.
        if (id.HasValue)
            query = query.Where(c => c.IdCondicaoPagamento == id.Value);

        if (!string.IsNullOrWhiteSpace(condicao))
        {
            var n = condicao.Trim();
            query = query.Where(c => c.DsCondicao.Contains(n));
        }

        var lista = await query
            .OrderBy(c => c.DsCondicao)
            .Take(100)
            .Select(c => new
            {
                id = c.IdCondicaoPagamento,
                condicao = c.DsCondicao,
                juro = c.VlJuro,
                multa = c.VlMulta,
                desconto = c.VlDesconto,
                ativo = c.FlAtivo,
                parcelas = c.Parcelas
                    .OrderBy(p => p.NrParcela)
                    .Select(p => new
                    {
                        nrParcela = p.NrParcela,
                        nrDiasVencimento = p.NrDiasVencimento,
                        idFormaPagamento = p.IdFormaPagamento,
                        formaNome = p.FormaPagamento != null ? p.FormaPagamento.DsFormaPagamento : null,
                        vlPercentual = p.VlPercentual
                    }).ToList()
            })
            .ToListAsync();

        return new JsonResult(lista);
    }
}
