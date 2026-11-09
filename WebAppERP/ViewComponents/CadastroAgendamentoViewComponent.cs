using Microsoft.AspNetCore.Mvc;
using WebAppERP.Business.Gerais;
using WebAppERP.ViewModels;

namespace WebAppERP.ViewComponents;

// Modal de cadastro de agendamento, reaproveitado pela tela de Agendamentos e
// pela pesquisa de agendamento da Venda (Novo / Editar).
// View: Pages/Shared/Components/CadastroAgendamento/Default.cshtml.
// Traz o proprio script (Default.cshtml.js, na mesma pasta), incluido uma vez
// por pagina. A gravacao fica em AgendamentoBusiness (chamado por AgendamentosModel).
public class CadastroAgendamentoViewComponent : ComponenteBase
{
    private readonly ClienteBusiness _cliente;
    private readonly FuncionarioBusiness _funcionario;
    private readonly ServicoBusiness _servico;

    public CadastroAgendamentoViewComponent(ClienteBusiness cliente, FuncionarioBusiness funcionario, ServicoBusiness servico)
    {
        _cliente = cliente;
        _funcionario = funcionario;
        _servico = servico;
    }

    // data: dia que o agendamento novo ja traz (padrao: hoje).
    public async Task<IViewComponentResult> InvokeAsync(string modo = "post", string? data = null)
    {
        var vm = new CadastroAgendamentoViewModel
        {
            Modo = modo == "ajax" ? "ajax" : "post",
            Clientes = await _cliente.Listar(),
            Funcionarios = await _funcionario.Listar(),
            Servicos = await _servico.Listar(somenteAtivos: true),
            DataPadrao = string.IsNullOrEmpty(data) ? DateTime.Today.ToString("yyyy-MM-dd") : data
        };

        // O script entra so na primeira vez que o componente aparece na pagina.
        vm.IncluirScript = PrimeiraVezNaPagina();

        return View(vm);
    }
}
