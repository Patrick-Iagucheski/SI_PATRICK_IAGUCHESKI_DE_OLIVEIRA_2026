using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;

namespace WebAppERP.Business.Operacional;

// Regras de negocio do agendamento: horario do funcionario sem choque e fluxo
// de situacoes (Agendado > Confirmado > Em atendimento > Concluido, ou Cancelado).
public class AgendamentoBusiness
{
    private readonly AppDbContext _db;

    public AgendamentoBusiness(AppDbContext db) => _db = db;

    // Proximas situacoes permitidas a partir de cada situacao.
    // Concluido e Cancelado sao finais.
    private static readonly Dictionary<string, string[]> Fluxo = new()
    {
        [Agendamento.AGENDADO] = [Agendamento.CONFIRMADO, Agendamento.CANCELADO],
        [Agendamento.CONFIRMADO] = [Agendamento.EM_ATENDIMENTO, Agendamento.CANCELADO],
        [Agendamento.EM_ATENDIMENTO] = [Agendamento.CONCLUIDO, Agendamento.CANCELADO],
        [Agendamento.CONCLUIDO] = [],
        [Agendamento.CANCELADO] = []
    };

    // ============================================================
    // Consultas
    // ============================================================

    // Agenda do dia.
    public async Task<List<Agendamento>> Listar(DateOnly data) =>
        await _db.Agendamentos
            .Include(a => a.Cliente)
            .Include(a => a.Funcionario)
            .Include(a => a.Servico)
            .Where(a => a.DtAgendamento == data)
            .OrderBy(a => a.HrInicio)
            .ToListAsync();

    // Agendamentos que podem originar uma venda: os mais recentes, sem os cancelados.
    public async Task<List<Agendamento>> ListarParaVenda() =>
        await _db.Agendamentos
            .Include(a => a.Cliente)
            .Where(a => a.TpStatus != Agendamento.CANCELADO)
            .OrderByDescending(a => a.DtAgendamento).ThenByDescending(a => a.HrInicio)
            .Take(100)
            .ToListAsync();

    // ============================================================
    // Gravacao
    // ============================================================

    // Inclui (IdAgendamento = 0) ou altera o agendamento e devolve o registro gravado.
    public async Task<Agendamento> Salvar(Agendamento agendamento)
    {
        var novo = agendamento.IdAgendamento == 0;
        Agendamento? registro = null;
        if (!novo)
        {
            registro = await _db.Agendamentos.FindAsync(agendamento.IdAgendamento)
                ?? throw new ValidacaoException("Agendamento não encontrado.", "Ele pode ter sido excluído por outra pessoa.");
            if (registro.TpStatus is Agendamento.CONCLUIDO or Agendamento.CANCELADO)
                throw new ValidacaoException("Agendamento concluído ou cancelado não pode ser alterado.");
        }

        var observacao = string.IsNullOrWhiteSpace(agendamento.DsObservacao) ? null : agendamento.DsObservacao.Trim();
        if (observacao?.Length > 500)
            throw new ValidacaoException("A observação deve ter no máximo 500 caracteres.");

        // Cadastro pela tela so entra como Agendado ou Confirmado; as demais
        // situacoes vem dos botoes da agenda (AlterarSituacao).
        var status = string.IsNullOrWhiteSpace(agendamento.TpStatus) ? Agendamento.AGENDADO : agendamento.TpStatus;
        if (novo && status is not (Agendamento.AGENDADO or Agendamento.CONFIRMADO))
            throw new ValidacaoException("Situação inválida para um novo agendamento.");
        if (!Fluxo.ContainsKey(status))
            throw new ValidacaoException("Situação do agendamento inválida.");

        // Cliente, funcionario e servico: precisam existir; inativos so sao
        // aceitos se ja eram os do agendamento (alteracao).
        var cliente = await _db.Clientes.FindAsync(agendamento.IdCliente);
        if (cliente == null)
            throw new ValidacaoException("Selecione o cliente.");
        if (!cliente.FlAtivo && registro?.IdCliente != cliente.IdCliente)
            throw new ValidacaoException("O cliente selecionado está inativo.");

        var funcionario = await _db.Funcionarios.FindAsync(agendamento.IdFuncionario);
        if (funcionario == null)
            throw new ValidacaoException("Selecione o funcionário.");
        if (!funcionario.FlAtivo && registro?.IdFuncionario != funcionario.IdFuncionario)
            throw new ValidacaoException("O funcionário selecionado está inativo.");

        var servico = await _db.Servicos.FindAsync(agendamento.IdServico);
        if (servico == null)
            throw new ValidacaoException("Selecione o serviço.");
        if (!servico.FlAtivo && registro?.IdServico != servico.IdServico)
            throw new ValidacaoException("O serviço selecionado está inativo.");

        // Data e horario
        if (agendamento.DtAgendamento == default)
            throw new ValidacaoException("Informe a data do agendamento.");
        if (novo && agendamento.DtAgendamento < DateOnly.FromDateTime(DateTime.Today))
            throw new ValidacaoException("Não é possível agendar em uma data que já passou.");

        var inicio = agendamento.HrInicio;
        var fim = agendamento.HrFim;
        if (fim == null && servico.NrDuracaoMin is > 0)
        {
            // Sem horario de fim: usa a duracao do servico (se nao virar o dia).
            var calculado = inicio.AddMinutes(servico.NrDuracaoMin.Value);
            if (calculado > inicio)
                fim = calculado;
        }
        if (fim != null && fim <= inicio)
            throw new ValidacaoException("O horário de fim deve ser depois do horário de início.");

        await ValidarConflito(agendamento.IdAgendamento, funcionario, agendamento.DtAgendamento, inicio, fim);

        if (registro == null)
        {
            registro = new Agendamento { DtCriacao = DateTime.Now };
            _db.Agendamentos.Add(registro);
        }
        else
        {
            registro.DtEdicao = DateTime.Now;
        }

        registro.IdCliente = cliente.IdCliente;
        registro.IdFuncionario = funcionario.IdFuncionario;
        registro.IdServico = servico.IdServico;
        registro.DtAgendamento = agendamento.DtAgendamento;
        registro.HrInicio = inicio;
        registro.HrFim = fim;
        registro.TpStatus = status;
        registro.DsObservacao = observacao;

        await _db.SaveChangesAsync();
        return registro;
    }

    // O funcionario nao pode ter dois atendimentos no mesmo horario
    // (agendamentos cancelados nao contam).
    private async Task ValidarConflito(int idAgendamento, Funcionario funcionario, DateOnly data, TimeOnly inicio, TimeOnly? fim)
    {
        var doDia = await _db.Agendamentos
            .Include(a => a.Servico)
            .Include(a => a.Cliente)
            .Where(a => a.IdFuncionario == funcionario.IdFuncionario
                     && a.DtAgendamento == data
                     && a.TpStatus != Agendamento.CANCELADO
                     && a.IdAgendamento != idAgendamento)
            .ToListAsync();

        var fimNovo = Fim(inicio, fim, null);
        var choque = doDia.FirstOrDefault(a => inicio < Fim(a.HrInicio, a.HrFim, a.Servico?.NrDuracaoMin) && a.HrInicio < fimNovo);
        if (choque != null)
            throw new ValidacaoException(
                $"{funcionario.NmFuncionario} já tem atendimento das {choque.HrInicio:HH\\:mm} às " +
                $"{Fim(choque.HrInicio, choque.HrFim, choque.Servico?.NrDuracaoMin):HH\\:mm} ({choque.Cliente?.NmCliente?.Trim()}).",
                "Escolha outro horário ou outro funcionário.");
    }

    // Fim do atendimento: o informado, senao inicio + duracao do servico,
    // senao um minuto (so o horario de inicio fica reservado).
    private static TimeOnly Fim(TimeOnly inicio, TimeOnly? fim, int? duracaoMin)
    {
        if (fim != null && fim > inicio) return fim.Value;
        var calculado = inicio.AddMinutes(duracaoMin is > 0 ? duracaoMin.Value : 1);
        return calculado > inicio ? calculado : TimeOnly.MaxValue;
    }

    // Avanca ou cancela o agendamento pelos botoes da agenda e devolve a
    // mensagem de sucesso.
    public async Task<string> AlterarSituacao(int id, string status)
    {
        var agendamento = await _db.Agendamentos.FindAsync(id)
            ?? throw new ValidacaoException("Agendamento não encontrado.", "Ele pode ter sido excluído por outra pessoa.");

        if (!Fluxo.TryGetValue(agendamento.TpStatus, out var proximas) || !proximas.Contains(status))
        {
            if (agendamento.TpStatus is Agendamento.CONCLUIDO or Agendamento.CANCELADO)
                throw new ValidacaoException("Agendamento concluído ou cancelado não pode mudar de situação.");
            throw new ValidacaoException("Mudança de situação inválida para este agendamento.");
        }

        agendamento.TpStatus = status;
        agendamento.DtEdicao = DateTime.Now;
        await _db.SaveChangesAsync();

        return status switch
        {
            Agendamento.CONFIRMADO => "Agendamento confirmado.",
            Agendamento.EM_ATENDIMENTO => "Atendimento iniciado.",
            Agendamento.CONCLUIDO => "Atendimento concluído.",
            _ => "Agendamento cancelado."
        };
    }

    public async Task Excluir(int id)
    {
        var agendamento = await _db.Agendamentos.FindAsync(id);
        if (agendamento == null)
            return;

        const string dica = "Cancele o agendamento em vez de excluí-lo.";

        // Filha: tbVenVendas.idAgendamento.
        if (await _db.Vendas.AnyAsync(v => v.IdAgendamento == id))
            throw new ValidacaoException("Não é possível excluir: existe venda gerada a partir deste agendamento.", dica);

        try
        {
            _db.Agendamentos.Remove(agendamento);
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new ValidacaoException("Não é possível excluir: existem registros vinculados a este agendamento.", dica);
        }
    }
}
