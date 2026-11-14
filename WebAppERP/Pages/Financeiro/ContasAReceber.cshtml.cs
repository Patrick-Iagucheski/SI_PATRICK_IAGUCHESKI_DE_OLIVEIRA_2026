using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebAppERP.Business.Financeiro;
using WebAppERP.Business.Gerais;

namespace WebAppERP.Pages.Financeiro;

// Tela de Contas a Receber: so conversa com a tela. As regras ficam em ContaAReceberBusiness.
public class ContasAReceberModel : PaginaBase
{
    private readonly ContaAReceberBusiness _conta;
    private readonly ClienteBusiness _cliente;
    private readonly FormaPagamentoBusiness _forma;

    public ContasAReceberModel(ContaAReceberBusiness conta, ClienteBusiness cliente, FormaPagamentoBusiness forma)
    {
        _conta = conta;
        _cliente = cliente;
        _forma = forma;
    }

    // Lista padrao da grade: so as pendentes (inclui as vencidas), o mesmo
    // valor inicial do filtro Situacao.
    public List<ContaAReceber> ContasGrade { get; set; } = [];
    public List<Cliente> Clientes { get; set; } = [];
    // Filtro da busca: todos os clientes, inclusive inativos (que ainda podem ter titulo).
    public List<Cliente> ClientesFiltro { get; set; } = [];
    public List<FormaPagamento> Formas { get; set; } = [];

    // Vendas canceladas entre as dos titulos da grade.
    public HashSet<int> VendasCanceladas { get; set; } = [];

    // Indicadores (sobre todas as contas)
    public int QtAberto { get; set; }
    public int QtVencidas { get; set; }
    public int QtRecebidas { get; set; }
    public decimal VlAberto { get; set; }
    public decimal VlVencidas { get; set; }
    public decimal VlRecebidas { get; set; }

    [BindProperty]
    public ContaAReceber ContaForm { get; set; } = new();

    public async Task OnGet()
    {
        ContasGrade = await _conta.Buscar(null, null, null, null, ContaAReceber.PENDENTE);
        ClientesFiltro = await _cliente.Listar(somenteAtivos: false);
        Clientes = ClientesFiltro.Where(c => c.FlAtivo).ToList();
        Formas = await _forma.Listar(somenteAtivas: true);
        VendasCanceladas = await _conta.VendasCanceladas(ContasGrade);

        var hoje = DateOnly.FromDateTime(DateTime.Today);
        foreach (var c in await _conta.Listar())
        {
            switch (ContaAReceberBusiness.GetSituacao(c, hoje))
            {
                case ContaAReceber.RECEBIDO: QtRecebidas++; VlRecebidas += c.VlRecebido; break;
                case ContaAReceber.VENCIDA: QtVencidas++; VlVencidas += c.VlValor; break;
                case ContaAReceber.PENDENTE: QtAberto++; VlAberto += c.VlValor; break;
            }
        }
    }

    public Task<IActionResult> OnPost() =>
        Executar(() => _conta.Salvar(ContaForm), ContaForm.IdContaReceber == 0 ? "Conta a receber cadastrada com sucesso." : "Conta a receber atualizada com sucesso.");

    // Baixa: marca como recebido na data de hoje.
    public Task<IActionResult> OnPostBaixa(int id) =>
        Executar(() => _conta.Baixar(id), "Baixa realizada com sucesso.");

    public Task<IActionResult> OnPostExcluir(int id) =>
        Executar(() => _conta.Excluir(id), "Conta a receber excluída com sucesso.");

    // ============================================================
    // Busca de contas a receber via AJAX (filtros da pagina)
    // Pesquisa por: codigo, conta (descricao), documento, cliente e situacao
    // ============================================================
    public async Task<IActionResult> OnGetBuscarContas(
        int? id, string? conta, string? documento, int? idCliente, string? situacao)
    {
        var lista = await _conta.Buscar(id, conta, documento, idCliente, situacao);
        var canceladas = await _conta.VendasCanceladas(lista);
        return new JsonResult(lista.Select(c => Registro(c, c.IdVenda is int v && canceladas.Contains(v))));
    }

    // Conta no formato que a tela usa para editar (editarConta no .cshtml.js):
    // o mesmo para a busca e para as linhas iniciais da grade.
    //   origemVenda: titulo gerado pela venda (cliente, valor, datas e descricao
    //                so mudam pela venda)
    //   bloqueado: venda cancelada ou recebido no ato da venda (nao muda mais)
    public static object Registro(ContaAReceber c, bool vendaCancelada) => new
    {
        id = c.IdContaReceber,
        registro = Module.Registro(c),
        conta = c.DsConta,
        documento = c.NrDocumento,
        idCliente = c.IdCliente,
        clienteNome = c.Cliente?.NmCliente,
        idForma = c.IdFormaPagamento,
        formaNome = c.FormaPagamento?.DsFormaPagamento,
        emissao = c.DtEmissao?.ToString("yyyy-MM-dd"),
        venc = c.DtVencimento?.ToString("yyyy-MM-dd"),
        receb = c.DtRecebimento?.ToString("yyyy-MM-dd"),
        valor = c.VlValor,
        recebido = c.VlRecebido,
        status = c.DsStatus,
        obs = c.DsObservacao,
        venda = c.IdVenda,
        origemVenda = c.OriginadoDeVenda,
        vendaCancelada,
        recebidoNaVenda = c.RecebidoNaVenda
    };
}
