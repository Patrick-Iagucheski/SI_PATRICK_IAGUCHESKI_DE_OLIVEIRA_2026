using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebAppERP.Business.Gerais;
using WebAppERP.Business.Operacional;

namespace WebAppERP.Pages.Operacional;

// Tela de Veiculos: so conversa com a tela. As regras ficam em VeiculoBusiness.
public class VeiculosModel : PageModel
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

    public async Task<IActionResult> OnPost()
    {
        try
        {
            var novo = VeiculoForm.IdVeiculo == 0;
            await _veiculo.Salvar(VeiculoForm);
            TempData["Sucesso"] = novo ? "Veículo cadastrado com sucesso." : "Veículo atualizado com sucesso.";
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
            await _veiculo.Excluir(id);
            TempData["Sucesso"] = "Veículo excluído com sucesso.";
        }
        catch (ValidacaoException ex)
        {
            TempData["Erro"] = ex.MensagemCompleta;
        }
        return RedirectToPage();
    }
}
