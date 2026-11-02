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

            // Preenche o cabecalho para os asp-for exibirem os valores atuais.
            IdVenda = venda.IdVenda;
            IdCliente = venda.IdCliente;
            IdFuncionario = venda.IdFuncionario;
            IdAgendamento = venda.IdAgendamento;
            IdFormaPagamento = venda.IdFormaPagamento;
            TpStatus = venda.TpStatus;
            VlDesconto = venda.VlDesconto;
            DsObservacao = venda.DsObservacao;
        }

        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        var itens = JsonSerializer.Deserialize<List<VendaItemDTO>>(ItensJson ?? "[]", JsonOpts) ?? [];

        var input = new VendaDTO
        {
            IdVenda = IdVenda,
            IdCliente = IdCliente,
            IdFuncionario = IdFuncionario,
            IdAgendamento = IdAgendamento,
            IdFormaPagamento = IdFormaPagamento,
            TpStatus = TpStatus,
            VlDesconto = VlDesconto,
            DsObservacao = DsObservacao,
            Itens = itens
        };

        try
        {
            await _business.Salvar(input);
            TempData["Sucesso"] = TpStatus == Venda.FINALIZADA
                ? "Venda finalizada. Estoque atualizado."
                : "Venda salva com sucesso.";
            return RedirectToPage("Vendas");
        }
        catch (Exception ex) when (MensagemDeErro(ex) is { } mensagem)
        {
            Erro = mensagem;
        }

        // Em caso de erro, recarrega combos e reexibe a tela com os dados digitados.
        Vm.Combos = await _business.GetCombos();
        Vm.Venda = new Venda
        {
            IdVenda = IdVenda,
            IdCliente = IdCliente,
            IdFuncionario = IdFuncionario,
            IdAgendamento = IdAgendamento,
            IdFormaPagamento = IdFormaPagamento,
            TpStatus = TpStatus,
            VlDesconto = VlDesconto,
            DsObservacao = DsObservacao,
            DtVenda = DateTime.Now
        };
        return Page();
    }
}
