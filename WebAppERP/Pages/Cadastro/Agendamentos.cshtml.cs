using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;
using WebAppERP.Models;

namespace WebAppERP.Pages.Cadastro;

public class AgendamentosModel : PageModel
{
    private readonly AppDbContext _db;

    public AgendamentosModel(AppDbContext db) => _db = db;

    public List<OpeAgendamento> Agendamentos { get; set; } = [];
    public List<GerCliente> Clientes { get; set; } = [];
    public List<GerFuncionario> Funcionarios { get; set; } = [];
    public List<GerServico> Servicos { get; set; } = [];

    public int QtdAgendados { get; set; }
    public int QtdConfirmados { get; set; }
    public int QtdEmAtendimento { get; set; }
    public int QtdConcluidos { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? DataFiltro { get; set; }

    [BindProperty]
    public OpeAgendamento AgendamentoForm { get; set; } = new();

    public async Task OnGetAsync()
    {
        var dataRef = DateOnly.FromDateTime(DateTime.Today);
        if (!string.IsNullOrEmpty(DataFiltro) && DateOnly.TryParse(DataFiltro, out var parsed))
            dataRef = parsed;
        else
            DataFiltro = dataRef.ToString("yyyy-MM-dd");

        Agendamentos = await _db.Agendamentos
            .Include(a => a.Cliente)
            .Include(a => a.Funcionario)
            .Include(a => a.Servico)
            .Where(a => a.DtAgendamento == dataRef)
            .OrderBy(a => a.HrInicio)
            .ToListAsync();

        QtdAgendados = Agendamentos.Count(a => a.TpStatus == "AGENDADO");
        QtdConfirmados = Agendamentos.Count(a => a.TpStatus == "CONFIRMADO");
        QtdEmAtendimento = Agendamentos.Count(a => a.TpStatus == "EM_ATENDIMENTO");
        QtdConcluidos = Agendamentos.Count(a => a.TpStatus == "CONCLUIDO");

        Clientes = await _db.Clientes.Where(c => c.FlAtivo).OrderBy(c => c.NmCliente).ToListAsync();
        Funcionarios = await _db.Funcionarios.Where(f => f.FlAtivo).OrderBy(f => f.NmFuncionario).ToListAsync();
        Servicos = await _db.Servicos.Where(s => s.FlAtivo).OrderBy(s => s.DsServico).ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (AgendamentoForm.IdAgendamento == 0)
        {
            AgendamentoForm.DtCriacao = DateTime.Now;
            _db.Agendamentos.Add(AgendamentoForm);
        }
        else
        {
            var existing = await _db.Agendamentos.FindAsync(AgendamentoForm.IdAgendamento);
            if (existing != null)
            {
                existing.IdCliente = AgendamentoForm.IdCliente;
                existing.IdFuncionario = AgendamentoForm.IdFuncionario;
                existing.IdServico = AgendamentoForm.IdServico;
                existing.DtAgendamento = AgendamentoForm.DtAgendamento;
                existing.HrInicio = AgendamentoForm.HrInicio;
                existing.HrFim = AgendamentoForm.HrFim;
                existing.TpStatus = AgendamentoForm.TpStatus;
                existing.DsObservacao = AgendamentoForm.DsObservacao;
                existing.DtEdicao = DateTime.Now;
            }
        }

        await _db.SaveChangesAsync();
        return RedirectToPage(new { DataFiltro = AgendamentoForm.DtAgendamento.ToString("yyyy-MM-dd") });
    }

    public async Task<IActionResult> OnPostStatusAsync(int id, string status, string? data)
    {
        var agendamento = await _db.Agendamentos.FindAsync(id);
        if (agendamento != null)
        {
            agendamento.TpStatus = status;
            agendamento.DtEdicao = DateTime.Now;
            await _db.SaveChangesAsync();
        }
        return RedirectToPage(new { DataFiltro = data });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, string? data)
    {
        var agendamento = await _db.Agendamentos.FindAsync(id);
        if (agendamento != null)
        {
            _db.Agendamentos.Remove(agendamento);
            await _db.SaveChangesAsync();
        }
        return RedirectToPage(new { DataFiltro = data });
    }
}
