using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebAppERP.Business.Financeiro;
using WebAppERP.Business.Gerais;

namespace WebAppERP.Pages.Financeiro;

// Tela de Contas a Pagar: so conversa com a tela. As regras ficam em ContaAPagarBusiness.
public class ContasAPagarModel : PaginaBase
{
    private readonly ContaAPagarBusiness _conta;
    private readonly FornecedorBusiness _fornecedor;
    private readonly FormaPagamentoBusiness _forma;

    public ContasAPagarModel(ContaAPagarBusiness conta, FornecedorBusiness fornecedor, FormaPagamentoBusiness forma)
    {
        _conta = conta;
        _fornecedor = fornecedor;
        _forma = forma;
    }

    public List<ContaAPagar> Contas { get; set; } = [];
    public List<Fornecedor> Fornecedores { get; set; } = [];
    public List<FormaPagamento> Formas { get; set; } = [];

    // Titulos de compra ja cancelada (abrem com todos os campos travados).
    public HashSet<int> IdsCompraCancelada { get; set; } = [];

    // Indicadores
    public int QtAberto { get; set; }
    public int QtVencidas { get; set; }
    public int QtPagas { get; set; }
    public decimal VlAberto { get; set; }
    public decimal VlVencidas { get; set; }
    public decimal VlPagas { get; set; }

    [BindProperty]
    public ContaAPagar ContaForm { get; set; } = new();

    // Lista padrao da grade: so as pendentes (inclui as vencidas), o mesmo
    // valor inicial do filtro Situacao. Os indicadores usam todas as contas.
    public IEnumerable<ContaAPagar> ContasGrade => Contas.Where(c => c.DsStatus == ContaAPagar.PENDENTE);

    public async Task OnGet()
    {
        Contas = await _conta.Listar();
        Fornecedores = await _fornecedor.Listar(somenteAtivos: false);
        Formas = await _forma.Listar(somenteAtivas: true);
        IdsCompraCancelada = await _conta.IdsDeCompraCancelada(Contas);

        var hoje = DateOnly.FromDateTime(DateTime.Today);
        foreach (var c in Contas)
        {
            switch (ContaAPagarBusiness.GetSituacao(c, hoje))
            {
                case ContaAPagar.PAGO: QtPagas++; VlPagas += c.VlPago; break;
                case ContaAPagar.VENCIDA: QtVencidas++; VlVencidas += c.VlValor; break;
                case ContaAPagar.PENDENTE: QtAberto++; VlAberto += c.VlValor; break;
            }
        }
    }

    public Task<IActionResult> OnPost() =>
        Executar(() => _conta.Salvar(ContaForm), ContaForm.IdContaPagar == 0 ? "Conta a pagar cadastrada com sucesso." : "Conta a pagar atualizada com sucesso.");

    // Baixa financeira: marca como pago na data de hoje.
    public Task<IActionResult> OnPostBaixa(int id) =>
        Executar(() => _conta.Baixar(id), "Baixa realizada com sucesso.");

    public Task<IActionResult> OnPostExcluir(int id) =>
        Executar(() => _conta.Excluir(id), "Conta a pagar excluída com sucesso.");

    // ============================================================
    // Busca de contas a pagar via AJAX (filtros da pagina)
    // Pesquisa por: codigo, conta (descricao), documento ou fornecedor
    // ============================================================
    public async Task<IActionResult> OnGetBuscarContas(
        int? id, string? conta, string? documento, int? idFornecedor, string? situacao)
    {
        var lista = await _conta.Buscar(id, conta, documento, idFornecedor, situacao);
        var canceladas = await _conta.IdsDeCompraCancelada(lista);
        return new JsonResult(lista.Select(c => Registro(c, canceladas.Contains(c.IdContaPagar))));
    }

    // Conta no formato que a tela usa para editar (editarConta no .cshtml.js):
    // o mesmo para a busca e para as linhas iniciais da grade.
    //   origemCompra: titulo gerado pela compra (fornecedor, valor, datas e
    //                 descricao so mudam pela compra)
    //   compraCancelada: a compra foi cancelada (o titulo nao muda mais)
    public static object Registro(ContaAPagar c, bool compraCancelada) => new
    {
        id = c.IdContaPagar,
        registro = Module.Registro(c),
        conta = c.DsConta,
        documento = c.NrDocumento,
        idFornecedor = c.IdFornecedor,
        fornecedorNome = c.Fornecedor?.DsRazaoSocial,
        idForma = c.IdFormaPagamento,
        formaNome = c.FormaPagamento?.DsFormaPagamento,
        emissao = c.DtEmissao?.ToString("yyyy-MM-dd"),
        venc = c.DtVencimento?.ToString("yyyy-MM-dd"),
        pgto = c.DtPagamento?.ToString("yyyy-MM-dd"),
        valor = c.VlValor,
        pago = c.VlPago,
        status = c.DsStatus,
        obs = c.DsObservacao,
        nota = c.NrNota,
        origemCompra = c.OriginadoDeCompra,
        compraCancelada
    };
}
