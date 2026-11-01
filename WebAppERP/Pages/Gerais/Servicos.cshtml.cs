using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebAppERP.Business.Gerais;

namespace WebAppERP.Pages.Gerais;

// Tela de Servicos: so conversa com a tela. As regras ficam em ServicoBusiness.
public class ServicosModel : PageModel
{
    private readonly ServicoBusiness _servico;

    public ServicosModel(ServicoBusiness servico) => _servico = servico;

    public List<Servico> Servicos { get; set; } = [];

    [BindProperty]
    public Servico ServicoForm { get; set; } = new();

    public async Task OnGet()
    {
        Servicos = await _servico.Listar();
    }

    public async Task<IActionResult> OnPost()
    {
        try
        {
            var novo = ServicoForm.IdServico == 0;
            await _servico.Salvar(ServicoForm);
            TempData["Sucesso"] = novo ? "Serviço cadastrado com sucesso." : "Serviço atualizado com sucesso.";
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
            await _servico.Excluir(id);
            TempData["Sucesso"] = "Serviço excluído com sucesso.";
        }
        catch (ValidacaoException ex)
        {
            TempData["Erro"] = ex.MensagemCompleta;
        }
        return RedirectToPage();
    }
}
