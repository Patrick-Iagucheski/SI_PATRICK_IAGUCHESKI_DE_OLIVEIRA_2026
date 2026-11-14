using Microsoft.AspNetCore.Mvc;
using WebAppERP.Business.Estoque;
using WebAppERP.DTOs.Estoque;

namespace WebAppERP.Pages.Estoque;

// Balancete de Estoque: so conversa com a tela. As contas ficam em
// MovimentacaoEstoqueBusiness.GetBalancete.
public class BalanceteEstoqueModel : PaginaBase
{
    private readonly MovimentacaoEstoqueBusiness _movimentacao;
    private readonly ProdutoBusiness _produto;

    public BalanceteEstoqueModel(MovimentacaoEstoqueBusiness movimentacao, ProdutoBusiness produto)
    {
        _movimentacao = movimentacao;
        _produto = produto;
    }

    // Filtros (pela URL: o balancete pode ser reaberto com o mesmo periodo)
    [BindProperty(SupportsGet = true)] public DateOnly? Inicio { get; set; }
    [BindProperty(SupportsGet = true)] public DateOnly? Fim { get; set; }
    [BindProperty(SupportsGet = true)] public int? Produto { get; set; }
    [BindProperty(SupportsGet = true)] public string? Saldo { get; set; }
    [BindProperty(SupportsGet = true)] public bool Movimento { get; set; }

    public List<BalanceteEstoqueDTO> Linhas { get; set; } = [];
    public List<Produto> Produtos { get; set; } = [];
    public string? Erro { get; set; }

    // Indicadores sobre as linhas listadas
    public decimal TotCompras => Linhas.Sum(l => l.QtCompras);
    public decimal TotVendas => Linhas.Sum(l => l.QtVendas);
    public decimal TotPerdas => Linhas.Sum(l => l.QtPerdas);
    public int QtNegativos => Linhas.Count(l => l.QtSaldoFinal < 0);
    public decimal VlEstoque => Linhas.Sum(l => l.VlEstoque);

    // Periodo padrao: do primeiro dia do mes ate hoje.
    public async Task OnGet()
    {
        var hoje = DateOnly.FromDateTime(DateTime.Today);
        Inicio ??= new DateOnly(hoje.Year, hoje.Month, 1);
        Fim ??= hoje;
        Produtos = await _produto.Listar();

        try
        {
            Linhas = await _movimentacao.GetBalancete(Inicio.Value, Fim.Value, Produto, Saldo, Movimento);
        }
        catch (Exception ex) when (MensagemDeErro(ex) is { } mensagem)
        {
            Erro = mensagem;
        }
    }
}
