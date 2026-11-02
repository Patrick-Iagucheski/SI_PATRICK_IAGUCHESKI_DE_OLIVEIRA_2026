using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebAppERP.Business.Gerais;

namespace WebAppERP.Pages.Gerais;

// Tela de Transportadores: so conversa com a tela. As regras ficam em TransportadorBusiness.
public class TransportadoresModel : PaginaBase
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

    public Task<IActionResult> OnPost() =>
        Executar(() => _transportador.Salvar(TransportadorForm), TransportadorForm.IdTransportador == 0 ? "Transportador cadastrado com sucesso." : "Transportador atualizado com sucesso.");

    public Task<IActionResult> OnPostExcluir(int id) =>
        Executar(() => _transportador.Excluir(id), "Transportador excluído com sucesso.");
}
