using Microsoft.AspNetCore.Mvc;
using WebAppERP.Business.Gerais;
using WebAppERP.ViewModels;

namespace WebAppERP.ViewComponents;

// Modal de cadastro de funcionario, reaproveitado pela tela de cadastro
// e pelas pesquisas de outras telas. View: Pages/Shared/Components/CadastroFuncionario/Default.cshtml.
// Traz o proprio script (Default.cshtml.js, na mesma pasta), incluido uma vez
// por pagina. A gravacao fica no Business (chamado pela tela /Gerais/Funcionarios).
public class CadastroFuncionarioViewComponent : ComponenteBase
{
    private readonly CargoBusiness _cargo;

    public CadastroFuncionarioViewComponent(CargoBusiness cargo) => _cargo = cargo;

    public async Task<IViewComponentResult> InvokeAsync(string modo = "post")
    {
        var vm = new CadastroFuncionarioViewModel
        {
            Modo = modo == "ajax" ? "ajax" : "post",
            Cargos = await _cargo.Listar()
        };

        // O script entra so na primeira vez que o componente aparece na pagina.
        vm.IncluirScript = PrimeiraVezNaPagina();

        return View(vm);
    }
}
