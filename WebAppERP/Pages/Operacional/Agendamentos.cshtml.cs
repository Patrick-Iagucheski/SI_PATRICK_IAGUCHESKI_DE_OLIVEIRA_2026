using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebAppERP.Business.Gerais;
using WebAppERP.Business.Operacional;

namespace WebAppERP.Pages.Operacional;

// Tela de Agendamentos: so conversa com a tela. As regras ficam em AgendamentoBusiness.
public class AgendamentosModel : PageModel
{
    private readonly AgendamentoBusiness _agendamento;
    private readonly ClienteBusiness _cliente;
    private readonly FuncionarioBusiness _funcionario;
    private readonly ServicoBusiness _servico;

    public AgendamentosModel(AgendamentoBusiness agendamento, ClienteBusiness cliente, FuncionarioBusiness funcionario, ServicoBusiness servico)
    {
        _agendamento = agendamento;
        _cliente = cliente;
        _funcionario = funcionario;
        _servico = servico;
    }

    public List<Agendamento> Agendamentos { get; set; } = [];
    public List<Cliente> Clientes { get; set; } = [];
    public List<Funcionario> Funcionarios { get; set; } = [];
    public List<Servico> Servicos { get; set; } = [];

    public int QtdAgendados { get; set; }
    public int QtdConfirmados { get; set; }
    public int QtdEmAtendimento { get; set; }
    public int QtdConcluidos { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? DataFiltro { get; set; }

    [BindProperty]
    public Agendamento AgendamentoForm { get; set; } = new();

    public async Task OnGet()
    {
        var dataRef = DateOnly.FromDateTime(DateTime.Today);
        if (!string.IsNullOrEmpty(DataFiltro) && DateOnly.TryParse(DataFiltro, out var parsed))
            dataRef = parsed;
        else
            DataFiltro = dataRef.ToString("yyyy-MM-dd");

        Agendamentos = await _agendamento.Listar(dataRef);

        QtdAgendados = Agendamentos.Count(a => a.TpStatus == Agendamento.AGENDADO);
        QtdConfirmados = Agendamentos.Count(a => a.TpStatus == Agendamento.CONFIRMADO);
        QtdEmAtendimento = Agendamentos.Count(a => a.TpStatus == Agendamento.EM_ATENDIMENTO);
        QtdConcluidos = Agendamentos.Count(a => a.TpStatus == Agendamento.CONCLUIDO);

        Clientes = await _cliente.Listar();
        Funcionarios = await _funcionario.Listar();
        Servicos = await _servico.Listar(somenteAtivos: true);
    }

    public async Task<IActionResult> OnPost()
    {
        try
        {
            var novo = AgendamentoForm.IdAgendamento == 0;
            await _agendamento.Salvar(AgendamentoForm);
            TempData["Sucesso"] = novo ? "Agendamento cadastrado com sucesso." : "Agendamento atualizado com sucesso.";
        }
        catch (ValidacaoException ex)
        {
            TempData["Erro"] = ex.MensagemCompleta;
        }

        // Volta para o dia do agendamento (ou para o dia que estava na tela, se a data nao veio).
        var data = AgendamentoForm.DtAgendamento == default ? DataFiltro : AgendamentoForm.DtAgendamento.ToString("yyyy-MM-dd");
        return RedirectToPage(new { DataFiltro = data });
    }

    public async Task<IActionResult> OnPostAlterarStatus(int id, string status, string? data)
    {
        try
        {
            TempData["Sucesso"] = await _agendamento.AlterarSituacao(id, status);
        }
        catch (ValidacaoException ex)
        {
            TempData["Erro"] = ex.MensagemCompleta;
        }
        return RedirectToPage(new { DataFiltro = data });
    }

    public async Task<IActionResult> OnPostExcluir(int id, string? data)
    {
        try
        {
            await _agendamento.Excluir(id);
            TempData["Sucesso"] = "Agendamento excluído com sucesso.";
        }
        catch (ValidacaoException ex)
        {
            TempData["Erro"] = ex.MensagemCompleta;
        }
        return RedirectToPage(new { DataFiltro = data });
    }
}
