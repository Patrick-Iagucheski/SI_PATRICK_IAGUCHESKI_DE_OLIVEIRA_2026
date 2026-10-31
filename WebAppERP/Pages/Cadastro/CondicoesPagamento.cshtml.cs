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

    // As formas de pagamento do modal sao carregadas pelo proprio componente
    // (ViewComponents/CadastroCondicaoPagamentoViewComponent).

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
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var erro = await ValidarEGravarAsync();
        if (erro != null) return await ComErroAsync(erro);

        return RedirectToPage();
    }

    // Cadastro pelo componente em modo AJAX (ex.: botao + da condicao no
    // cadastro de fornecedor). Mesmas regras do OnPostAsync, mas responde JSON
    // para a tela de origem nao ser recarregada.
    public async Task<IActionResult> OnPostSalvarAjaxAsync()
    {
        var erro = await ValidarEGravarAsync();
        if (erro != null) return new JsonResult(new { sucesso = false, mensagem = erro });

        return new JsonResult(new
        {
            sucesso = true,
            id = CondicaoForm.IdCondicaoPagamento,
            nome = CondicaoForm.DsCondicao,
            ativo = CondicaoForm.FlAtivo
        });
    }

    // Valida a condicao e as parcelas (ParcelasJson) e inclui/atualiza.
    // Retorna a mensagem do primeiro erro, ou null se gravou. Na inclusao,
    // CondicaoForm.IdCondicaoPagamento recebe o id gerado.
    private async Task<string?> ValidarEGravarAsync()
    {
        var parcelas = JsonSerializer.Deserialize<List<ParcelaDto>>(ParcelasJson ?? "[]", JsonOpts) ?? [];

        // ===== Validacoes de servidor (defensivas; a tela ja valida) =====
        if (string.IsNullOrWhiteSpace(CondicaoForm.DsCondicao))
            return "Informe a condição de pagamento.";

        if (parcelas.Count == 0)
            return "Adicione ao menos uma parcela.";

        if (parcelas.Any(p => p.IdFormaPagamento <= 0))
            return "Selecione a forma de pagamento em todas as parcelas.";

        if (parcelas.Any(p => p.VlPercentual <= 0 || p.VlPercentual > 100))
            return "O percentual de cada parcela deve estar entre 0 e 100.";

        if (parcelas.Any(p => p.NrDiasVencimento < 0))
            return "Os dias para vencimento não podem ser negativos.";

        var soma = parcelas.Sum(p => p.VlPercentual);
        if (Math.Abs(soma - 100m) > 0.01m)
            return $"O percentual total das parcelas deve ser 100%. Atual: {soma:N2}%.";

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
            await _db.SaveChangesAsync();

            CondicaoForm.IdCondicaoPagamento = nova.IdCondicaoPagamento;
            CondicaoForm.DsCondicao = nova.DsCondicao;
            return null;
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
        CondicaoForm.DsCondicao = nome;
        return null;
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
        if (c == null)
            return RedirectToPage();

        try
        {
            // Parcelas saem por cascade (configurado no DbContext / FK).
            _db.CondicoesPagamento.Remove(c);
            await _db.SaveChangesAsync();
            TempData["Sucesso"] = "Condição de pagamento excluída com sucesso.";
        }
        catch (DbUpdateException)
        {
            TempData["Erro"] = "Não é possível excluir: existem registros vinculados a esta condição de pagamento.";
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
