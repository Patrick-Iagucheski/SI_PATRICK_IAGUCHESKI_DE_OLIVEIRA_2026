using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebAppERP.Business.Gerais;

namespace WebAppERP.Pages.Gerais;

// Tela de Transportadores: so conversa com a tela. As regras ficam em TransportadorBusiness.
public class TransportadoresModel : PageModel
{
    private readonly TransportadorBusiness _transportador;

    public TransportadoresModel(TransportadorBusiness transportador) => _transportador = transportador;

    public List<Transportador> Transportadores { get; set; } = [];

    [BindProperty]
    public Transportador TransportadorForm { get; set; } = new();

    public async Task OnGet()
    {
        Transportadores = await _transportador.Listar();
    }

    public async Task<IActionResult> OnPost()
    {
        try
        {
            var novo = TransportadorForm.IdTransportador == 0;
            await _transportador.Salvar(TransportadorForm);
            TempData["Sucesso"] = novo ? "Transportador cadastrado com sucesso." : "Transportador atualizado com sucesso.";
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
            await _transportador.Excluir(id);
            TempData["Sucesso"] = "Transportador excluído com sucesso.";
        }
        catch (ValidacaoException ex)
        {
            TempData["Erro"] = ex.MensagemCompleta;
        }
        return RedirectToPage();
    }
}
