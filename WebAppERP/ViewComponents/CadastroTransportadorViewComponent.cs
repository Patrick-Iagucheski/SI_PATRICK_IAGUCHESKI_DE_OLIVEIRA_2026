using Microsoft.AspNetCore.Mvc;
using WebAppERP.ViewModels;

namespace WebAppERP.ViewComponents;

// Modal de cadastro de transportadora, reaproveitado pela tela de
// Transportadores e pela pesquisa de transportadora (lupa do campo da Compra).
// A cidade usa o componente unico CampoCidade.
// View: Pages/Shared/Components/CadastroTransportador/Default.cshtml.
// Traz o proprio script (Default.cshtml.js, na mesma pasta), incluido uma vez
// por pagina. A gravacao fica em TransportadorBusiness (chamado por TransportadoresModel).
public class CadastroTransportadorViewComponent : ComponenteBase
{
    public IViewComponentResult Invoke(string modo = "post")
    {
        var vm = new CadastroTransportadorViewModel { Modo = modo == "ajax" ? "ajax" : "post" };

        // O script entra so na primeira vez que o componente aparece na pagina.
        vm.IncluirScript = PrimeiraVezNaPagina();

        return View(vm);
    }
}
