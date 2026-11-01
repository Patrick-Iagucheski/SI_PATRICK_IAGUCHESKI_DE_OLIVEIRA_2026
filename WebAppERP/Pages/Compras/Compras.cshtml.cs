using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebAppERP.DTOs.Compras;
using WebAppERP.Business.Compras;

namespace WebAppERP.Pages.Compras;

// Lista de compras: so conversa com a tela. As regras ficam em CompraBusiness.
public class ComprasModel : PageModel
{
    private readonly CompraBusiness _business;

    public ComprasModel(CompraBusiness business) => _business = business;

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
        try
        {
            await _business.Cancelar(GetChave(nrNota, nrSerie, nrModelo, idFornecedor));
            TempData["Sucesso"] = "Compra cancelada. Estoque estornado e títulos cancelados.";
        }
        catch (ValidacaoException ex)
        {
            TempData["Erro"] = ex.MensagemCompleta;
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostExcluir(int nrNota, int nrSerie, int nrModelo, int idFornecedor)
    {
        try
        {
            await _business.Excluir(GetChave(nrNota, nrSerie, nrModelo, idFornecedor));
            TempData["Sucesso"] = "Compra excluída com sucesso.";
        }
        catch (ValidacaoException ex)
        {
            TempData["Erro"] = ex.MensagemCompleta;
        }
        return RedirectToPage();
    }
}
