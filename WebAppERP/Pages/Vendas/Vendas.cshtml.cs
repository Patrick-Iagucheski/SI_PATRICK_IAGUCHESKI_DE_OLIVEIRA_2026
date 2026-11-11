using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebAppERP.Business.Vendas;

namespace WebAppERP.Pages.Vendas;

// Lista de vendas: so conversa com a tela. As regras ficam em VendaBusiness.
public class VendasModel : PaginaBase
{
    private readonly VendaBusiness _business;

    public VendasModel(VendaBusiness business) => _business = business;

    public List<Venda> Vendas { get; set; } = [];

    public async Task OnGet()
    {
        Vendas = await _business.Listar();
    }

    public Task<IActionResult> OnPostCancelar(int id) =>
        Executar(() => _business.Cancelar(id), "Venda cancelada. Estoque devolvido e títulos do Contas a Receber cancelados.");

    public Task<IActionResult> OnPostExcluir(int id) =>
        Executar(() => _business.Excluir(id), "Venda excluída com sucesso.");
}
