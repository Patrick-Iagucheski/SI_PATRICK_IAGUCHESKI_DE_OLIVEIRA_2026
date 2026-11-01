using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebAppERP.Business.Seguranca;
using WebAppERP.Infrastructure;

namespace WebAppERP.Pages.Seguranca;

// Configuracoes do sistema: nome e logo da empresa exibidos no menu lateral.
// So conversa com a tela. As regras (e a logo) ficam em EmpresaBusiness.
public class ConfiguracoesModel : PageModel
{
    private readonly EmpresaBusiness _empresa;
    private readonly EmpresaService _empresaLayout;

    public ConfiguracoesModel(EmpresaBusiness empresa, EmpresaService empresaLayout)
    {
        _empresa = empresa;
        _empresaLayout = empresaLayout;
    }

    [BindProperty]
    public string NmEmpresa { get; set; } = string.Empty;

    // Caminho da logo atual; vazio quando o usuario remove a logo (volta a padrao)
    [BindProperty]
    public string? DsLogo { get; set; }

    [BindProperty]
    public IFormFile? LogoUpload { get; set; }

    public bool TabelaCriada { get; set; } = true;

    public async Task OnGet()
    {
        try
        {
            var empresa = await _empresa.GetEmpresa();
            NmEmpresa = empresa?.NmEmpresa ?? EmpresaService.NomePadrao;
            DsLogo = empresa?.DsLogo;
        }
        catch (Exception)
        {
            // Tabela ainda nao criada: a tela avisa para rodar o script.
            TabelaCriada = false;
            NmEmpresa = EmpresaService.NomePadrao;
        }
    }

    public async Task<IActionResult> OnPost()
    {
        try
        {
            await _empresa.Salvar(NmEmpresa, LogoUpload, removerLogo: string.IsNullOrEmpty(DsLogo));
            // Menu lateral, titulo e favicon passam a mostrar o novo nome/logo.
            _empresaLayout.Limpar();
            TempData["Sucesso"] = "Configurações salvas com sucesso.";
        }
        catch (ValidacaoException ex)
        {
            TempData["Erro"] = ex.MensagemCompleta;
        }
        return RedirectToPage();
    }
}
