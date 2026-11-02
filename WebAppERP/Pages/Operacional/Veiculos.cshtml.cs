using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebAppERP.Business.Gerais;
using WebAppERP.Business.Operacional;

namespace WebAppERP.Pages.Operacional;

// Tela de Veiculos: so conversa com a tela. As regras ficam em VeiculoBusiness.
public class VeiculosModel : PaginaBase
{
    private readonly VeiculoBusiness _veiculo;
    private readonly EstadoBusiness _estado;

    public VeiculosModel(VeiculoBusiness veiculo, EstadoBusiness estado)
    {
        _veiculo = veiculo;
        _estado = estado;
    }

    public List<Veiculo> Veiculos { get; set; } = [];
    public List<Estado> Estados { get; set; } = [];

    [BindProperty]
    public Veiculo VeiculoForm { get; set; } = new();

    public async Task OnGet()
    {
        Veiculos = await _veiculo.Listar();
        Estados = await _estado.Listar();
    }

    public Task<IActionResult> OnPost() =>
        Executar(() => _veiculo.Salvar(VeiculoForm), VeiculoForm.IdVeiculo == 0 ? "Veículo cadastrado com sucesso." : "Veículo atualizado com sucesso.");

    public Task<IActionResult> OnPostExcluir(int id) =>
        Executar(() => _veiculo.Excluir(id), "Veículo excluído com sucesso.");
}
