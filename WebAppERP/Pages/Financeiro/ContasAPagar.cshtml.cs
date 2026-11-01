using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebAppERP.Business.Financeiro;
using WebAppERP.Business.Gerais;

namespace WebAppERP.Pages.Financeiro;

// Tela de Contas a Pagar: so conversa com a tela. As regras ficam em ContaAPagarBusiness.
public class ContasAPagarModel : PageModel
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

    // Indicadores
    public int QtAberto { get; set; }
    public int QtVencidas { get; set; }
    public int QtPagas { get; set; }
    public decimal VlAberto { get; set; }
    public decimal VlVencidas { get; set; }
    public decimal VlPagas { get; set; }

    [BindProperty]
    public ContaAPagar ContaForm { get; set; } = new();

    public async Task OnGet()
    {
        Contas = await _conta.Listar();
        Fornecedores = await _fornecedor.Listar(somenteAtivos: false);
        Formas = await _forma.Listar(somenteAtivas: true);

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

    public async Task<IActionResult> OnPost()
    {
        try
        {
            var nova = ContaForm.IdContaPagar == 0;
            await _conta.Salvar(ContaForm);
            TempData["Sucesso"] = nova ? "Conta a pagar cadastrada com sucesso." : "Conta a pagar atualizada com sucesso.";
        }
        catch (ValidacaoException ex)
        {
            TempData["Erro"] = ex.MensagemCompleta;
        }
        return RedirectToPage();
    }

    // Baixa financeira: marca como pago na data de hoje.
    public async Task<IActionResult> OnPostBaixa(int id)
    {
        try
        {
            await _conta.Baixar(id);
            TempData["Sucesso"] = "Baixa realizada com sucesso.";
        }
        catch (ValidacaoException ex)
        {
            TempData["Erro"] = ex.MensagemCompleta;
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostExcluir(int id)
    {
        try
        {
            await _conta.Excluir(id);
            TempData["Sucesso"] = "Conta a pagar excluída com sucesso.";
        }
        catch (ValidacaoException ex)
        {
            TempData["Erro"] = ex.MensagemCompleta;
        }
        return RedirectToPage();
    }

    // ============================================================
    // Busca de contas a pagar via AJAX (filtros da pagina)
    // Pesquisa por: codigo, conta (descricao), documento ou fornecedor
    // ============================================================
    public async Task<IActionResult> OnGetBuscarContas(
        int? id, string? conta, string? documento, int? idFornecedor, bool incluirInativos = false)
    {
        var lista = await _conta.Buscar(id, conta, documento, idFornecedor, incluirInativos);
        return new JsonResult(lista.Select(c => new
        {
            id = c.IdContaPagar,
            conta = c.DsConta,
            documento = c.NrDocumento,
            idFornecedor = c.IdFornecedor,
            fornecedorNome = c.Fornecedor?.DsRazaoSocial,
            idForma = c.IdFormaPagamento,
            formaNome = c.FormaPagamento?.DsFormaPagamento,
            emissao = c.DtEmissao,
            venc = c.DtVencimento,
            pgto = c.DtPagamento,
            valor = c.VlValor,
            pago = c.VlPago,
            status = c.DsStatus,
            ativo = c.FlAtivo,
            obs = c.DsObservacao
        }));
    }
}
