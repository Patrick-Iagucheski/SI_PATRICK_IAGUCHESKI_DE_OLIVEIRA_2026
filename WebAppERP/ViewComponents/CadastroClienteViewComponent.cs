using Microsoft.AspNetCore.Mvc;
using WebAppERP.ViewModels;

namespace WebAppERP.ViewComponents;

// Modal de cadastro de cliente, reaproveitado pela tela de cadastro
// e pelas pesquisas de outras telas. View: Pages/Shared/Components/CadastroCliente/Default.cshtml.
// Traz o proprio script (Default.cshtml.js, na mesma pasta), incluido uma vez
// por pagina. A gravacao fica no Business (chamado pela tela /Gerais/Clientes).
public class CadastroClienteViewComponent : ComponenteBase
{
    public IViewComponentResult Invoke(string modo = "post")
    {
        var vm = new CadastroClienteViewModel { Modo = modo == "ajax" ? "ajax" : "post" };

        // O script entra so na primeira vez que o componente aparece na pagina.
        vm.IncluirScript = PrimeiraVezNaPagina();

        return View(vm);
    }
}
