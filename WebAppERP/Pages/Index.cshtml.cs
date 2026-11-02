using Microsoft.AspNetCore.Mvc.RazorPages;
using WebAppERP.Business.Principal;
using WebAppERP.DTOs.Principal;

namespace WebAppERP.Pages;

// Tela Principal: so conversa com a tela. O painel e montado em PrincipalBusiness.
public class IndexModel : PaginaBase
{
    private readonly PrincipalBusiness _principal;

    public IndexModel(PrincipalBusiness principal) => _principal = principal;

    public PainelDTO Painel { get; set; } = new();

    public async Task OnGet()
    {
        Painel = await _principal.GetPainel(DateOnly.FromDateTime(DateTime.Today));
    }
}
