// Script do componente de cadastro de agendamento
// (Pages/Shared/Components/CadastroAgendamento/Default.cshtml).
// Usado pela tela de Agendamentos e pela pesquisa de agendamento da Venda.

// Texto da opcao de servico no agendamento (igual ao da lista do servidor).
function textoServicoAgenda(s) {
    return s.nome + ' - R$ ' + moeda(s.valor) + ' (' + (s.duracao || 0) + ' min)';
}

// ===== Horario fim = inicio + duracao do servico =====
// Sugerido ao trocar o servico ou o horario de inicio (pode ser mudado a mao).
// Servico sem duracao: o fim fica como esta. Se passaria da meia-noite, o fim
// fica em branco (um fim antigo, antes do inicio, seria recusado ao salvar).
function duracaoServicoAgenda() {
    const opt = document.getElementById('agServico').selectedOptions[0];
    return parseInt(opt?.dataset.duracao) || 0;
}

function sugerirHorarioFim() {
    const inicio = document.getElementById('agHrInicio').value;
    const duracao = duracaoServicoAgenda();
    if (!inicio || !duracao) return;
    const [h, m] = inicio.split(':').map(Number);
    const fim = h * 60 + m + duracao;
    const dois = n => String(n).padStart(2, '0');
    document.getElementById('agHrFim').value = fim >= 24 * 60 ? '' : dois(Math.floor(fim / 60)) + ':' + dois(fim % 60);
}

document.getElementById('agServico').addEventListener('change', sugerirHorarioFim);
document.getElementById('agHrInicio').addEventListener('change', sugerirHorarioFim);

// Lupa do servico: alem de colocar no campo, guarda a duracao na opcao (o
// servico cadastrado ou editado pela pesquisa pode nao estar na lista da tela).
function abrirBuscaServicoAgenda() {
    const campo = opcoesDoSelect('agServico', textoServicoAgenda);
    const guardarDuracao = s => {
        const opt = document.querySelector('#agServico option[value="' + s.id + '"]');
        if (opt) opt.dataset.duracao = s.duracao || 0;
    };
    abrirPesquisaServico({
        ...campo,
        selecionar: s => { campo.selecionar(s); guardarDuracao(s); sugerirHorarioFim(); },
        atualizado: s => { campo.atualizado(s); guardarDuracao(s); }
    });
}

// ===== Novo / Editar =====
const SITUACOES_AGENDAMENTO = { AGENDADO: 'Agendado', CONFIRMADO: 'Confirmado', EM_ATENDIMENTO: 'Em atendimento', CONCLUIDO: 'Concluído', CANCELADO: 'Cancelado' };

function limparFormAgendamento() {
    document.getElementById('agTitulo').innerHTML = '<i class="bi bi-calendar-plus me-2"></i>Novo Agendamento';
    document.getElementById('aId').value = '0';
    ['agCliente', 'agFuncionario', 'agServico', 'agHrInicio', 'agHrFim', 'agObservacao'].forEach(id => {
        document.getElementById(id).value = '';
    });
    document.getElementById('agData').value = document.getElementById('agData').defaultValue || hojeIso();
    document.getElementById('agStatus').value = 'AGENDADO';
}

// a: agendamento da pesquisa (AgendamentosModel.Registro). Cliente, profissional
// e servico entram no campo mesmo se nao estiverem na lista (ex.: inativos).
function editarAgendamento(a) {
    document.getElementById('agTitulo').innerHTML = '<i class="bi bi-pencil me-2"></i>Editar Agendamento';
    document.getElementById('aId').value = a.id;
    selecionarNoSelect('agCliente', a.idCliente, a.cliente);
    selecionarNoSelect('agFuncionario', a.idFuncionario, a.profissional);
    selecionarNoSelect('agServico', a.idServico, textoServicoAgenda({ nome: a.servico, valor: a.servicoValor, duracao: a.servicoDuracao }));
    const opt = document.querySelector('#agServico option[value="' + a.idServico + '"]');
    if (opt) opt.dataset.duracao = a.servicoDuracao || 0;
    document.getElementById('agData').value = a.data;
    document.getElementById('agHrInicio').value = a.hora;
    document.getElementById('agHrFim').value = a.horaFim || '';
    // Situacao fora do cadastro (ex.: Em atendimento): entra na lista para
    // nao trocar a situacao ao salvar.
    const status = document.getElementById('agStatus');
    if (!status.querySelector('option[value="' + a.situacao + '"]'))
        status.add(new Option(SITUACOES_AGENDAMENTO[a.situacao] || a.situacao, a.situacao));
    status.value = a.situacao;
    document.getElementById('agObservacao').value = a.observacao || '';
}

// ===== Pesquisa de agendamento (o + do campo de agendamento da Venda) =====
// Mesmos filtros da tela de Agendamentos, ja na data de hoje. Cancelado
// aparece, mas nao pode ser escolhido (a venda nao aceita).
function urlAgendamentos(handler) {
    return document.getElementById('modalAgendamento').dataset.urlAgendamentos + '?handler=' + handler;
}

const PESQUISA_AGENDAMENTO = {
    chave: 'agendamento',
    titulo: 'Pesquisar Agendamento',
    url: () => urlAgendamentos('BuscarAgendamentos'),
    filtros: [
        { param: 'id', rotulo: 'ID', tipo: 'number', col: 2 },
        { param: 'data', rotulo: 'Data', tipo: 'date', col: 3, padrao: hojeIso() },
        { param: 'texto', rotulo: 'Cliente / Serviço / Profissional', col: 4 },
        { param: 'situacao', rotulo: 'Situação', col: 3,
          opcoes: [{ valor: '', texto: 'Todas' }].concat(Object.entries(SITUACOES_AGENDAMENTO).map(([valor, texto]) => ({ valor, texto }))) }
    ],
    colunas: ['Código', 'Data / Hora', 'Cliente', 'Serviço', 'Profissional', 'Situação'],
    celulas: a => ['<strong>' + a.id + '</strong>', dataBr(a.data) + ' ' + escaparHtml(a.hora),
                   escaparHtml(a.cliente || '-'), escaparHtml(a.servico || '-'), escaparHtml(a.profissional || '-'),
                   escaparHtml(SITUACOES_AGENDAMENTO[a.situacao] || a.situacao)],
    modal: 'modalAgendamento',
    evento: 'agendamento:salvo',
    preencher: editarAgendamento,
    limpar: limparFormAgendamento,
    urlExcluir: () => urlAgendamentos('Excluir'),
    nome: a => a.nome
};

function abrirPesquisaAgendamento(opcoes) {
    abrirPesquisaComCadastro(PESQUISA_AGENDAMENTO, opcoes);
}
