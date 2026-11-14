using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebAppERP.DTOs.Vendas;
using WebAppERP.Business.Vendas;
using WebAppERP.ViewModels;

namespace WebAppERP.Pages.Vendas;

// Tela de Venda (inclusao e edicao): so conversa com a tela. As regras ficam em VendaBusiness.
public class VendaModel : PaginaBase
{
    private readonly VendaBusiness _business;

    public VendaModel(VendaBusiness business) => _business = business;

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public VendaEditorViewModel Vm { get; set; } = new();
    public string? Erro { get; set; }

    // Cabecalho (bind direto das propriedades da pagina).
    [BindProperty] public int IdVenda { get; set; }
    [BindProperty] public int? IdCliente { get; set; }
    [BindProperty] public int IdFuncionario { get; set; }
    [BindProperty] public int? IdAgendamento { get; set; }
    [BindProperty] public int? IdFormaPagamento { get; set; }
    [BindProperty] public int? IdCondicaoPagamento { get; set; }
    [BindProperty] public string TpStatus { get; set; } = Venda.ABERTA;
    [BindProperty] public decimal VlDesconto { get; set; }
    [BindProperty] public string? DsObservacao { get; set; }

    // Itens serializados em JSON pela tela.
    [BindProperty] public string ItensJson { get; set; } = "[]";

    public async Task<IActionResult> OnGet(int? id)
    {
        Vm.Combos = await _business.GetCombos();

        if (id is > 0)
        {
            var venda = await _business.GetVendaComItens(id.Value);
            if (venda == null) return RedirectToPage("Vendas");

            Vm.Venda = venda;
            Vm.Itens = venda.Itens;
            Vm.Agendamento = await _business.GetAgendamento(venda.IdAgendamento);

            // Preenche o cabecalho para os asp-for exibirem os valores atuais.
            IdVenda = venda.IdVenda;
            IdCliente = venda.IdCliente;
            IdFuncionario = venda.IdFuncionario;
            IdAgendamento = venda.IdAgendamento;
            IdFormaPagamento = venda.IdFormaPagamento;
            IdCondicaoPagamento = venda.IdCondicaoPagamento;
            TpStatus = venda.TpStatus;
            VlDesconto = venda.VlDesconto;
            DsObservacao = venda.DsObservacao;
        }

        return Page();
    }

    // Condicao de pagamento do cliente escolhido (a tela sugere na venda).
    public async Task<IActionResult> OnGetCondicaoDoCliente(int idCliente)
    {
        var condicao = await _business.GetCondicaoDoCliente(idCliente);
        return new JsonResult(new { id = condicao?.IdCondicaoPagamento, nome = condicao?.DsCondicao });
    }

    // Botao Atualizar: listas e precos atuais dos cadastros, sem gravar a venda.
    public async Task<IActionResult> OnGetAtualizarDados(int? id)
    {
        var combos = await _business.GetCombos();
        return new JsonResult(new
        {
            emAberto = await _business.GetSituacao(id) == Venda.ABERTA,
            clientes = combos.Clientes.Select(c => new { id = c.IdCliente, nome = c.NmCliente, detalhe = Module.DocumentoPessoa(c) }),
            funcionarios = combos.Funcionarios.Select(f => new { id = f.IdFuncionario, nome = f.NmFuncionario }),
            agendamentos = combos.Agendamentos.Select(a => new { id = a.IdAgendamento, nome = TextoAgendamento(a) }),
            formas = combos.Formas.Select(f => new { id = f.IdFormaPagamento, nome = f.DsFormaPagamento }),
            condicoes = combos.Condicoes.Select(c => new { id = c.IdCondicaoPagamento, nome = c.DsCondicao }),
            produtos = ProdutosDaTela(combos.Produtos),
            servicos = ServicosDaTela(combos.Servicos)
        });
    }

    // Formatos usados tanto ao abrir a tela quanto no botao Atualizar.
    public static string TextoAgendamento(Agendamento a) =>
        $"#{a.IdAgendamento} - {a.DtAgendamento:dd/MM/yyyy} {a.HrInicio:HH:mm} - {a.Cliente?.NmCliente}";

    public static IEnumerable<object> ProdutosDaTela(IEnumerable<Produto> produtos) =>
        produtos.Select(p => new { id = p.IdProduto, nome = p.DsProduto, saldo = p.QtSaldo ?? 0, valor = p.VlVenda ?? 0, imagem = p.DsImagem });

    public static IEnumerable<object> ServicosDaTela(IEnumerable<Servico> servicos) =>
        servicos.Select(s => new { id = s.IdServico, nome = s.DsServico, valor = s.VlServico });

    // Itens enviados pela tela. Texto fora do formato (ex.: numero invalido)
    // vira mensagem, sem derrubar a pagina.
    private List<VendaItemDTO> ItensDaTela()
    {
        List<VendaItemDTO?>? itens;
        try
        {
            itens = JsonSerializer.Deserialize<List<VendaItemDTO?>>(string.IsNullOrWhiteSpace(ItensJson) ? "[]" : ItensJson, JsonOpts);
        }
        catch (JsonException)
        {
            itens = [null];
        }
        if (itens == null || itens.Any(i => i == null))
            throw new ValidacaoException("Os itens da venda chegaram em formato inválido.", "Recarregue a tela e confira os itens.");
        return itens.Select(i => i!).ToList();
    }

    private VendaDTO MontarDados(List<VendaItemDTO> itens) => new()
    {
        IdVenda = IdVenda,
        IdCliente = IdCliente,
        IdFuncionario = IdFuncionario,
        IdAgendamento = IdAgendamento,
        IdFormaPagamento = IdFormaPagamento,
        IdCondicaoPagamento = IdCondicaoPagamento,
        TpStatus = TpStatus,
        VlDesconto = VlDesconto,
        DsObservacao = DsObservacao,
        Itens = itens
    };

    // Salvamento automatico da venda aberta: devolve o numero da venda, para a
    // tela continuar alterando a mesma venda.
    public async Task<IActionResult> OnPostAutoSalvar()
    {
        try
        {
            var id = await _business.SalvarAberta(MontarDados(ItensDaTela()));
            return new JsonResult(new { ok = true, erro = (string?)null, id, hora = DateTime.Now.ToString("HH:mm:ss") });
        }
        catch (Exception ex) when (MensagemDeErro(ex) is { } mensagem)
        {
            return new JsonResult(new { ok = false, erro = mensagem });
        }
    }

    public async Task<IActionResult> OnPost()
    {
        List<VendaItemDTO> itens = [];

        try
        {
            itens = ItensDaTela();
            await _business.Salvar(MontarDados(itens));
            TempData["Sucesso"] = TpStatus == Venda.FINALIZADA
                ? "Venda finalizada. Estoque atualizado e títulos gerados no Contas a Receber."
                : "Venda salva como aberta.";
            return RedirectToPage("Vendas");
        }
        catch (Exception ex) when (MensagemDeErro(ex) is { } mensagem)
        {
            Erro = mensagem;
        }

        // Em caso de erro, recarrega combos e reexibe a tela com os dados e os
        // itens digitados (o salvamento automatico nao pode gravar a grade vazia).
        var combos = await _business.GetCombos();
        Vm.Combos = combos;
        Vm.Itens = itens.Select(i => new VendaItem
        {
            TpItem = i.TpItem,
            IdServico = i.IdServico,
            IdProduto = i.IdProduto,
            QtItem = i.QtItem,
            VlUnitario = i.VlUnitario,
            VlDesconto = i.VlDesconto,
            VlTotal = Math.Max(i.QtItem * i.VlUnitario - i.VlDesconto, 0)
        }).ToList();
        Vm.Venda = new Venda
        {
            IdVenda = IdVenda,
            IdCliente = IdCliente,
            IdFuncionario = IdFuncionario,
            IdAgendamento = IdAgendamento,
            IdFormaPagamento = IdFormaPagamento,
            IdCondicaoPagamento = IdCondicaoPagamento,
            TpStatus = await _business.GetSituacao(IdVenda) ?? Venda.ABERTA,   // a gravacao falhou: vale a situacao do banco
            VlDesconto = VlDesconto,
            DsObservacao = DsObservacao,
            DtVenda = DateTime.Now
        };
        // Cliente, funcionario, forma, condicao, produtos e servicos inativos nao
        // estao nas listas: vem pelo id, para continuarem no formulario.
        await _business.CarregarVinculos(Vm.Venda, Vm.Itens);
        Vm.Agendamento = await _business.GetAgendamento(IdAgendamento);
        return Page();
    }
}
