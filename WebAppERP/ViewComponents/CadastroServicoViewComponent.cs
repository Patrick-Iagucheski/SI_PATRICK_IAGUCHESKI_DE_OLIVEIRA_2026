using Microsoft.AspNetCore.Mvc;
using WebAppERP.Business.Gerais;
using WebAppERP.ViewModels;

namespace WebAppERP.ViewComponents;

// Modal de cadastro de servico, reaproveitado pela tela de cadastro
// e pelas pesquisas de outras telas. View: Pages/Shared/Components/CadastroServico/Default.cshtml.
// Traz o proprio script (Default.cshtml.js, na mesma pasta), incluido uma vez
// por pagina. A gravacao fica no Business (chamado pela tela /Gerais/Servicos).
public class CadastroServicoViewComponent : ComponenteBase
{
    private readonly CategoriaServicoBusiness _categoria;

    public CadastroServicoViewComponent(CategoriaServicoBusiness categoria) => _categoria = categoria;

    public async Task<IViewComponentResult> InvokeAsync(string modo = "post")
    {
        var vm = new CadastroServicoViewModel
        {
            Modo = modo == "ajax" ? "ajax" : "post",
            Categorias = await _categoria.Listar()
        };

        // O script entra so na primeira vez que o componente aparece na pagina.
        vm.IncluirScript = PrimeiraVezNaPagina();

        return View(vm);
    }
}
