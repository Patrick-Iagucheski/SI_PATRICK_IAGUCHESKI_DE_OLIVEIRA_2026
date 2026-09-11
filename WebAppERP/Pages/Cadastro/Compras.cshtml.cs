using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebAppERP.DTOs;
using WebAppERP.Models;
using WebAppERP.Repositories;

namespace WebAppERP.Pages.Cadastro;

public class ComprasModel : PageModel
{
    private readonly ICompraRepository _repo;

    public ComprasModel(ICompraRepository repo) => _repo = repo;

    public List<OpeCompra> Compras { get; set; } = [];

    public async Task OnGetAsync()
    {
        Compras = await _repo.ListarAsync();
    }

    private static CompraChaveDto Chave(int nrNota, int nrSerie, int nrModelo, int idFornecedor) => new()
    {
        NrNota = nrNota,
        NrSerie = nrSerie,
        NrModelo = nrModelo,
        IdFornecedor = idFornecedor
    };

    public async Task<IActionResult> OnPostCancelarAsync(int nrNota, int nrSerie, int nrModelo, int idFornecedor)
    {
        var (ok, erro) = await _repo.CancelarAsync(Chave(nrNota, nrSerie, nrModelo, idFornecedor));

        if (ok) TempData["Sucesso"] = "Compra cancelada. Estoque estornado e títulos cancelados.";
        else TempData["Erro"] = erro;

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int nrNota, int nrSerie, int nrModelo, int idFornecedor)
    {
        var (ok, erro) = await _repo.ExcluirAsync(Chave(nrNota, nrSerie, nrModelo, idFornecedor));

        if (ok) TempData["Sucesso"] = "Compra excluída com sucesso.";
        else TempData["Erro"] = erro;

        return RedirectToPage();
    }
}
