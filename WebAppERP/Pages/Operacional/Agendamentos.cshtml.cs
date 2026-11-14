using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebAppERP.Business.Operacional;

namespace WebAppERP.Pages.Operacional;

// Tela de Agendamentos: so conversa com a tela. As regras ficam em AgendamentoBusiness.
public class AgendamentosModel : PaginaBase
{
    private readonly AgendamentoBusiness _agendamento;

    // Listas do modal (clientes, profissionais, servicos) vem do componente
    // CadastroAgendamento.
    public AgendamentosModel(AgendamentoBusiness agendamento) => _agendamento = agendamento;

    public List<Agendamento> Agendamentos { get; set; } = [];

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
    }

    public Task<IActionResult> OnPost()
    {
        // Volta para o dia do agendamento (ou para o dia que estava na tela, se a data nao veio).
        var data = AgendamentoForm.DtAgendamento == default ? DataFiltro : AgendamentoForm.DtAgendamento.ToString("yyyy-MM-dd");
        return Executar(() => _agendamento.Salvar(AgendamentoForm),
            AgendamentoForm.IdAgendamento == 0 ? "Agendamento cadastrado com sucesso." : "Agendamento atualizado com sucesso.",
            new { DataFiltro = data });
    }

    // Botoes da agenda (Confirmar, Iniciar, Concluir, Cancelar).
    public Task<IActionResult> OnPostAlterarSituacao(int id, string status, string? data) =>
        ExecutarComMensagem(() => _agendamento.AlterarSituacao(id, status), new { DataFiltro = data });

    // Cadastro pelo componente em modo AJAX (Novo / Editar da pesquisa de
    // agendamento da Venda). Mesmas regras do OnPost, mas responde JSON.
    public Task<IActionResult> OnPostSalvarAjax() =>
        ExecutarJson(async () =>
        {
            var salvo = await _agendamento.Salvar(AgendamentoForm);
            // Recarrega com cliente, profissional e servico para o texto do campo.
            var a = (await _agendamento.Pesquisar(salvo.IdAgendamento, null, null, null)).First();
            return new { sucesso = true, id = a.IdAgendamento, nome = Texto(a), ativo = a.TpStatus != Agendamento.CANCELADO };
        });

    // Pesquisa do agendamento (campo de agendamento da Venda), com os filtros
    // da tela.
    public async Task<IActionResult> OnGetBuscarAgendamentos(int? id, DateOnly? data, string? texto, string? situacao)
    {
        var lista = await _agendamento.Pesquisar(id, data, texto, situacao);
        var vendas = await _agendamento.VendasDosAgendamentos(lista.Select(a => a.IdAgendamento));
        return new JsonResult(lista.Select(a => Registro(a, vendas.TryGetValue(a.IdAgendamento, out var v) ? v : null)));
    }

    // Agendamento no formato que o componente usa (pesquisa e editarAgendamento).
    // Cancelado ou ja cobrado em outra venda (idVenda) nao pode ser escolhido na
    // Venda (ativo false).
    // Texto do agendamento no campo da Venda (mesmo das opcoes da lista).
    public static string Texto(Agendamento a) =>
        $"#{a.IdAgendamento} - {a.DtAgendamento:dd/MM/yyyy} {a.HrInicio:HH:mm} - {a.Cliente?.NmCliente}";

    public static object Registro(Agendamento a, int? idVenda) => new
    {
        id = a.IdAgendamento,
        registro = Module.Registro(a),
        data = a.DtAgendamento.ToString("yyyy-MM-dd"),
        hora = a.HrInicio.ToString("HH:mm"),
        horaFim = a.HrFim?.ToString("HH:mm"),
        idCliente = a.IdCliente,
        cliente = a.Cliente?.NmCliente,
        idFuncionario = a.IdFuncionario,
        profissional = a.Funcionario?.NmFuncionario,
        idServico = a.IdServico,
        servico = a.Servico?.DsServico,
        servicoValor = a.Servico?.VlServico,
        servicoDuracao = a.Servico?.NrDuracaoMin,
        situacao = a.TpStatus,
        observacao = a.DsObservacao,
        nome = Texto(a),
        idVenda,
        ativo = a.TpStatus != Agendamento.CANCELADO && idVenda == null,
        motivoInativo = a.TpStatus == Agendamento.CANCELADO ? "Cancelado" : "Venda Nº " + idVenda
    };

    public Task<IActionResult> OnPostExcluir(int id, string? data) =>
        Executar(() => _agendamento.Excluir(id), "Agendamento excluído com sucesso.", new { DataFiltro = data });
}
