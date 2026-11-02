using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebAppERP.Business.Financeiro;

namespace WebAppERP.Pages.Financeiro;

// Tela de Condicoes de Pagamento: so conversa com a tela. As regras ficam em
// CondicaoPagamentoBusiness.
public class CondicoesPagamentoModel : PaginaBase
{
    private readonly CondicaoPagamentoBusiness _condicao;

    public CondicoesPagamentoModel(CondicaoPagamentoBusiness condicao) => _condicao = condicao;

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public List<CondicaoPagamento> Condicoes { get; set; } = [];

    // As formas de pagamento do modal sao carregadas pelo proprio componente
    // (ViewComponents/CadastroCondicaoPagamentoViewComponent).

    [BindProperty] public CondicaoPagamento CondicaoForm { get; set; } = new();
    [BindProperty] public string ParcelasJson { get; set; } = "[]";

    // DTO recebido da tela (parcelas serializadas em JSON).
    public class ParcelaDto
    {
        public int NrParcela { get; set; }
        public int NrDiasVencimento { get; set; }
        public int IdFormaPagamento { get; set; }
        public decimal VlPercentual { get; set; }
    }

    public async Task OnGet()
    {
        Condicoes = await _condicao.Listar();
    }

    // Parcelas da tela para o Business.
    private List<CondicaoPagamentoParcela> ParcelasDaTela() =>
        (JsonSerializer.Deserialize<List<ParcelaDto>>(ParcelasJson ?? "[]", JsonOpts) ?? [])
            .Select(p => new CondicaoPagamentoParcela
            {
                NrParcela = p.NrParcela,
                NrDiasVencimento = p.NrDiasVencimento,
                IdFormaPagamento = p.IdFormaPagamento,
                VlPercentual = p.VlPercentual
            })
            .ToList();

    // No erro, recarrega a tela e reabre o cadastro com o que foi digitado.
    public Task<IActionResult> OnPost() =>
        ExecutarOuReexibir(() => _condicao.Salvar(CondicaoForm, ParcelasDaTela()),
            CondicaoForm.IdCondicaoPagamento == 0 ? "Condição de pagamento cadastrada com sucesso." : "Condição de pagamento atualizada com sucesso.",
            "ErroCondicao", OnGet);

    // Cadastro pelo componente em modo AJAX (ex.: botao + da condicao no
    // cadastro de fornecedor e na Compra). Mesmas regras do OnPost, mas
    // responde JSON para a tela de origem nao ser recarregada.
    public Task<IActionResult> OnPostSalvarAjax() =>
        ExecutarJson(async () =>
        {
            var condicao = await _condicao.Salvar(CondicaoForm, ParcelasDaTela());
            return new
            {
                sucesso = true,
                id = condicao.IdCondicaoPagamento,
                nome = condicao.DsCondicao,
                ativo = condicao.FlAtivo
            };
        });

    public Task<IActionResult> OnPostAlterarSituacao(int id) =>
        ExecutarComMensagem(() => _condicao.AlterarSituacao(id));

    public Task<IActionResult> OnPostExcluir(int id) =>
        Executar(() => _condicao.Excluir(id), "Condição de pagamento excluída com sucesso.");

    // ============================================================
    // Busca de condicoes de pagamento via AJAX (filtros da pagina)
    // Pesquisa por: codigo ou condicao (descricao)
    // ============================================================
    public async Task<IActionResult> OnGetBuscarCondicoes(int? id, string? condicao, bool incluirInativos = false)
    {
        var lista = await _condicao.Buscar(id, condicao, incluirInativos);
        return new JsonResult(lista.Select(c => new
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
                    formaNome = p.FormaPagamento?.DsFormaPagamento,
                    vlPercentual = p.VlPercentual
                })
        }));
    }
}
