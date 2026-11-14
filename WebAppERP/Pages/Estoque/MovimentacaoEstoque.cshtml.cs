using Microsoft.AspNetCore.Mvc;
using WebAppERP.Business.Estoque;

namespace WebAppERP.Pages.Estoque;

// Movimentacao de Estoque: so consulta. O estoque entra pela compra e sai pela
// venda; nao ha lancamento manual. As regras ficam em MovimentacaoEstoqueBusiness.
public class MovimentacaoEstoqueModel : PaginaBase
{
    private readonly MovimentacaoEstoqueBusiness _movimentacao;
    private readonly ProdutoBusiness _produto;

    public MovimentacaoEstoqueModel(MovimentacaoEstoqueBusiness movimentacao, ProdutoBusiness produto)
    {
        _movimentacao = movimentacao;
        _produto = produto;
    }

    // Filtros (pela URL: o Balancete abre esta tela ja filtrada pelo produto)
    [BindProperty(SupportsGet = true)] public DateOnly? Inicio { get; set; }
    [BindProperty(SupportsGet = true)] public DateOnly? Fim { get; set; }
    [BindProperty(SupportsGet = true)] public int? Produto { get; set; }
    [BindProperty(SupportsGet = true)] public string? Origem { get; set; }
    [BindProperty(SupportsGet = true)] public string? Tipo { get; set; }

    public List<MovimentacaoEstoque> Movimentos { get; set; } = [];
    public List<Produto> Produtos { get; set; } = [];
    public string? Erro { get; set; }

    // Indicadores sobre os movimentos listados
    public decimal QtEntradas => Movimentos.Where(m => m.TpMovimento == MovimentacaoEstoque.ENTRADA).Sum(m => m.QtMovimento);
    public decimal QtSaidas => Movimentos.Where(m => m.TpMovimento == MovimentacaoEstoque.SAIDA).Sum(m => m.QtMovimento);

    // Periodo padrao: do primeiro dia do mes ate hoje.
    public async Task OnGet()
    {
        var hoje = DateOnly.FromDateTime(DateTime.Today);
        Inicio ??= new DateOnly(hoje.Year, hoje.Month, 1);
        Fim ??= hoje;
        Produtos = await _produto.Listar();

        try
        {
            Movimentos = await _movimentacao.Buscar(Inicio.Value, Fim.Value, Produto, Origem, Tipo);
        }
        catch (Exception ex) when (MensagemDeErro(ex) is { } mensagem)
        {
            Erro = mensagem;
        }
    }

    // Documento de origem do movimento, para a grade.
    public static string Documento(MovimentacaoEstoque m) =>
        m.IdVenda != null ? $"Venda Nº {m.IdVenda}"
        : m.NrNota != null ? $"Compra NF {m.NrModelo}/{m.NrSerie}/{m.NrNota}"
        : "-";
}
