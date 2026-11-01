using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebAppERP.DTOs.Compras;
using WebAppERP.Business.Compras;

namespace WebAppERP.Pages.Compras;

public class ComprasModel : PageModel
{
    private readonly CompraBusiness _business;

    public ComprasModel(CompraBusiness repo) => _business = repo;

    public List<Compra> Compras { get; set; } = [];

    public async Task OnGet()
    {
        Compras = await _business.Listar();
    }

    private static CompraChaveDTO GetChave(int nrNota, int nrSerie, int nrModelo, int idFornecedor) => new()
    {
        NrNota = nrNota,
        NrSerie = nrSerie,
        NrModelo = nrModelo,
        IdFornecedor = idFornecedor
    };

    public async Task<IActionResult> OnPostCancelar(int nrNota, int nrSerie, int nrModelo, int idFornecedor)
    {
        var (ok, erro) = await _business.Cancelar(GetChave(nrNota, nrSerie, nrModelo, idFornecedor));

        if (ok) TempData["Sucesso"] = "Compra cancelada. Estoque estornado e títulos cancelados.";
        else TempData["Erro"] = erro;

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostExcluir(int nrNota, int nrSerie, int nrModelo, int idFornecedor)
    {
        var (ok, erro) = await _business.Excluir(GetChave(nrNota, nrSerie, nrModelo, idFornecedor));

        if (ok) TempData["Sucesso"] = "Compra excluída com sucesso.";
        else TempData["Erro"] = erro;

        return RedirectToPage();
    }
}
