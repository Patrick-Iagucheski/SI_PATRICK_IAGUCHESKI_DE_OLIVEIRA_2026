using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebAppERP.Business.Gerais;

namespace WebAppERP.Pages.Gerais;

// Tela de Estados: so conversa com a tela. As regras ficam em EstadoBusiness.
public class EstadosModel : PaginaBase
{
    private readonly EstadoBusiness _estado;
    private readonly PaisBusiness _pais;

    public EstadosModel(EstadoBusiness estado, PaisBusiness pais)
    {
        _estado = estado;
        _pais = pais;
    }

    public List<Estado> Estados { get; set; } = [];
    public List<Pais> Paises { get; set; } = [];

    [BindProperty]
    public Estado EstadoForm { get; set; } = new();

    public async Task OnGet()
    {
        Estados = await _estado.Listar();
        Paises = await _pais.Listar();
    }

    public Task<IActionResult> OnPost() =>
        Executar(() => _estado.Salvar(EstadoForm), EstadoForm.IdEstado == 0 ? "Estado cadastrado com sucesso." : "Estado atualizado com sucesso.");

    public Task<IActionResult> OnPostExcluir(int id) =>
        Executar(() => _estado.Excluir(id), "Estado excluído com sucesso.");
}
