// Script da tela Contas a Receber (Pages/Financeiro/ContasAReceber.cshtml).

// ===== Busca de Contas a Receber (filtros na pagina -> atualiza a grid via AJAX) =====
let resultadosBusca = [];

// Enter em qualquer filtro dispara a busca.
['buscaId', 'buscaConta', 'buscaDocumento'].forEach(id => {
    document.getElementById(id)?.addEventListener('keydown', e => {
        if (e.key === 'Enter') { e.preventDefault(); buscarContas(); }
    });
});

function situacaoConta(c) {
    if (c.status === 'RECEBIDO') return 'RECEBIDO';
    if (c.status === 'CANCELADO') return 'CANCELADO';
    const hoje = hojeIso();
    if (c.venc && c.venc < hoje) return 'VENCIDA';
    return 'PENDENTE';
}
function badgeSituacao(sit) {
    const map = { 'RECEBIDO': 'badge-concluido', 'VENCIDA': 'badge-cancelado', 'CANCELADO': 'badge-inativo' };
    return '<span class="badge-status ' + (map[sit] || 'badge-agendado') + '">' + sit + '</span>';
}

function montarLinha(c, i) {
    const sit = situacaoConta(c);
    const docHtml = c.documento ? '<div class="text-muted small">Doc: ' + escaparHtml(c.documento) + '</div>' : '';
    const baixaHtml = (sit !== 'RECEBIDO' && sit !== 'CANCELADO')
        ? '<form method="post" action="?handler=Baixa" class="d-inline" onsubmit="return confirm(\'Confirmar baixa (recebimento) desta conta hoje?\')">' +
              campoToken() +
              '<input type="hidden" name="id" value="' + c.id + '">' +
              '<button type="submit" class="btn btn-icon btn-outline-success" title="Dar baixa / Marcar como recebido"><i class="bi bi-cash-coin"></i></button>' +
          '</form> '
        : '';
    return '<tr>' +
        '<td><strong>' + escaparHtml(c.conta || '') + '</strong>' + docHtml + '</td>' +
        '<td>' + escaparHtml(c.clienteNome || '-') + '</td>' +
        '<td>' + dataBr(c.venc) + '</td>' +
        '<td class="text-end"><strong>R$ ' + moeda(c.valor) + '</strong></td>' +
        '<td class="text-end">R$ ' + moeda(c.recebido) + '</td>' +
        '<td>' + badgeSituacao(sit) + '</td>' +
        '<td>' + escaparHtml(c.formaNome || '-') + '</td>' +
        '<td class="text-center" style="white-space: nowrap;">' +
            baixaHtml +
            '<button class="btn btn-icon btn-outline-primary" title="Editar" onclick="abrirEdicao(' + i + ')"><i class="bi bi-pencil"></i></button> ' +
            '<form method="post" action="?handler=Excluir" class="d-inline" onsubmit="return confirm(\'Deseja realmente excluir esta conta?\')">' +
                campoToken() +
                '<input type="hidden" name="id" value="' + c.id + '">' +
                '<button type="submit" class="btn btn-icon btn-outline-danger" title="Excluir"><i class="bi bi-trash"></i></button>' +
            '</form>' +
        '</td>' +
    '</tr>';
}

async function buscarContas() {
    const tbody = document.querySelector('#tblContas tbody');
    const id = document.getElementById('buscaId').value.trim();
    const conta = document.getElementById('buscaConta').value.trim();
    const documento = document.getElementById('buscaDocumento').value.trim();
    const idCliente = document.getElementById('buscaCliente').value;
    const situacao = document.getElementById('buscaSituacao').value;

    const params = new URLSearchParams();
    if (id) params.append('id', id);
    if (conta) params.append('conta', conta);
    if (documento) params.append('documento', documento);
    if (idCliente) params.append('idCliente', idCliente);
    params.append('situacao', situacao);

    tbody.innerHTML = '<tr><td colspan="8" class="text-center text-muted py-3">Buscando...</td></tr>';
    try {
        const resp = await fetch('?handler=BuscarContas&' + params.toString());
        resultadosBusca = await resp.json();

        const contador = document.getElementById('contadorRegistros');
        if (contador) contador.textContent = resultadosBusca.length + ' registros encontrados';

        if (!resultadosBusca.length) {
            tbody.innerHTML = '<tr><td colspan="8" class="text-center text-muted py-4">Nenhuma conta a receber encontrada</td></tr>';
            return;
        }
        tbody.innerHTML = resultadosBusca.map((c, i) => montarLinha(c, i)).join('');
    } catch (e) {
        tbody.innerHTML = '<tr><td colspan="8" class="text-center text-danger py-4">Erro ao buscar. Tente novamente.</td></tr>';
    }
}

function abrirEdicao(i) {
    mostrarRegistro('modalConta', resultadosBusca[i].registro);
    editarConta(resultadosBusca[i]);
    bootstrap.Modal.getOrCreateInstance(document.getElementById('modalConta')).show();
}

// Titulo gerado pela venda: cliente, valor, datas, descricao e documento so
// mudam pela venda; de venda cancelada ou recebido no ato da venda, nada muda
// (as mesmas regras de ContaAReceberBusiness.Salvar, que continua validando).
// c null: conta nova, tudo liberado.
const CAMPOS_DA_VENDA = ['contaDsConta', 'contaNrDocumento', 'contaIdCliente', 'contaDtEmissao', 'contaDtVencimento', 'contaVlValor'];
const CAMPOS_DO_RECEBIMENTO = ['contaIdFormaPagamento', 'contaDtRecebimento', 'contaVlRecebido', 'contaDsStatus', 'contaDsObservacao'];

function travarCampoConta(id, travar) {
    const el = document.getElementById(id);
    if (el.dataset.pesquisa !== undefined) el.dataset.somenteLeitura = travar ? '1' : '';   // campo com X e +
    else if (el.tagName === 'SELECT') el.disabled = travar;
    else el.readOnly = travar;
    el.classList.toggle('bg-light', travar);
}

function travarCamposConta(c) {
    const daVenda = !!c?.origemVenda;
    const bloqueado = daVenda && (c.vendaCancelada || c.recebidoNaVenda);
    CAMPOS_DA_VENDA.forEach(id => travarCampoConta(id, daVenda));
    CAMPOS_DO_RECEBIMENTO.forEach(id => travarCampoConta(id, bloqueado));
    document.getElementById('contaBtnSalvar').disabled = bloqueado;

    const venda = daVenda ? 'venda Nº ' + c.venda : '';
    const aviso = document.getElementById('contaAvisoVenda');
    aviso.classList.toggle('d-none', !daVenda);
    aviso.innerHTML = !daVenda ? '' : '<i class="bi bi-info-circle me-1"></i>' + (
        c.vendaCancelada ? 'Título da ' + venda + ', que foi cancelada: não pode ser alterado.'
        : c.recebidoNaVenda ? 'Título recebido no ato da ' + venda + ': não pode ser alterado.'
        : 'Título gerado pela ' + venda + ': cliente, valor, datas e descrição não podem ser alterados.');
}

// Cadastro inativado depois de lancado o titulo: a lista do formulario so
// tem ativos, entao o vinculo que o titulo ja tem entra como opcao, para nao
// sumir nem se perder ao salvar. O servidor aceita o vinculo que o titulo ja
// tinha e recusa escolher um inativo novo.
function removerOpcoesInativas(id) {
    document.querySelectorAll('#' + id + ' option[data-inativo]').forEach(o => o.remove());
}

function manterOpcaoInativa(id, valor, texto) {
    removerOpcoesInativas(id);
    const sel = document.getElementById(id);
    if (valor != null && valor !== '' && !sel.querySelector('option[value="' + valor + '"]')) {
        const opt = new Option((texto || 'Cadastro') + ' (inativo)', valor);
        opt.dataset.inativo = '1';
        sel.add(opt);
    }
    sel.value = valor ?? '';
}

function limparFormConta() {
    removerOpcoesInativas('contaIdCliente');
    removerOpcoesInativas('contaIdFormaPagamento');
    document.getElementById('contaTitulo').innerHTML = '<i class="bi bi-wallet2 me-2"></i>Nova Conta a Receber';
    document.getElementById('contaIdConta').value = '0';
    document.getElementById('contaCodigoDisplay').value = '';
    document.getElementById('contaDsConta').value = '';
    document.getElementById('contaNrDocumento').value = '';
    document.getElementById('contaIdCliente').value = '';
    document.getElementById('contaIdFormaPagamento').value = '';
    document.getElementById('contaDtEmissao').value = '';
    document.getElementById('contaDtVencimento').value = '';
    document.getElementById('contaDtRecebimento').value = '';
    document.getElementById('contaDsStatus').value = 'PENDENTE';
    document.getElementById('contaVlValor').value = '0';
    document.getElementById('contaVlRecebido').value = '0';
    document.getElementById('contaDsObservacao').value = '';
    travarCamposConta(null);
}

// c: conta no formato de ContasAReceberModel.Registro
function editarConta(c) {
    document.getElementById('contaTitulo').innerHTML = '<i class="bi bi-pencil me-2"></i>Editar Conta a Receber';
    document.getElementById('contaIdConta').value = c.id;
    document.getElementById('contaCodigoDisplay').value = c.id;
    document.getElementById('contaDsConta').value = c.conta || '';
    document.getElementById('contaNrDocumento').value = c.documento || '';
    manterOpcaoInativa('contaIdCliente', c.idCliente, c.clienteNome);
    manterOpcaoInativa('contaIdFormaPagamento', c.idForma, c.formaNome);
    document.getElementById('contaDtEmissao').value = c.emissao || '';
    document.getElementById('contaDtVencimento').value = c.venc || '';
    document.getElementById('contaDtRecebimento').value = c.receb || '';
    document.getElementById('contaDsStatus').value = c.status || 'PENDENTE';
    document.getElementById('contaVlValor').value = c.valor;
    document.getElementById('contaVlRecebido').value = c.recebido;
    document.getElementById('contaDsObservacao').value = c.obs || '';
    travarCamposConta(c);
}

// Situacao e pagamento andam juntos (mesma regra do servidor): voltar para
// Pendente limpa o valor e a data; marcar como Recebido preenche o valor
// total da conta (recebimento parcial nao e aceito) e a data de hoje quando vazia.
document.addEventListener('DOMContentLoaded', () => {
    document.getElementById('contaDsStatus').addEventListener('change', e => {
        const valor = document.getElementById('contaVlRecebido'), data = document.getElementById('contaDtRecebimento');
        if (e.target.value === 'PENDENTE') { valor.value = '0'; data.value = ''; }
        else if (e.target.value === 'RECEBIDO') {
            valor.value = document.getElementById('contaVlValor').value;   // recebido e o total (sem parcial)
            if (!data.value) data.value = hojeIso();
        }
    });
});
