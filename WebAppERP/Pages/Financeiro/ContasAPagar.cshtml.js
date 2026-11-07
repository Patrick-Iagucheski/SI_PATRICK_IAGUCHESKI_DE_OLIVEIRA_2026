// Script da tela Contas a Pagar (Pages/Financeiro/ContasAPagar.cshtml).

// ===== Busca de Contas a Pagar (filtros na pagina -> atualiza a grid via AJAX) =====
let resultadosBusca = [];

// Enter em qualquer filtro dispara a busca.
['buscaId', 'buscaConta', 'buscaDocumento'].forEach(id => {
    document.getElementById(id)?.addEventListener('keydown', e => {
        if (e.key === 'Enter') { e.preventDefault(); buscarContas(); }
    });
});

function situacaoConta(c) {
    if (c.status === 'PAGO') return 'PAGO';
    if (c.status === 'CANCELADO') return 'CANCELADO';
    const hoje = new Date().toISOString().slice(0, 10);
    if (c.venc && c.venc < hoje) return 'VENCIDA';
    return 'PENDENTE';
}
function badgeSituacao(sit) {
    const map = { 'PAGO': 'badge-concluido', 'VENCIDA': 'badge-cancelado', 'CANCELADO': 'badge-inativo' };
    return '<span class="badge-status ' + (map[sit] || 'badge-agendado') + '">' + sit + '</span>';
}

function montarLinha(c, i) {
    const sit = situacaoConta(c);
    const docHtml = c.documento ? '<div class="text-muted small">Doc: ' + escaparHtml(c.documento) + '</div>' : '';
    const baixaHtml = (sit !== 'PAGO' && sit !== 'CANCELADO')
        ? '<form method="post" action="?handler=Baixa" class="d-inline" onsubmit="return confirm(\'Confirmar baixa (pagamento) desta conta hoje?\')">' +
              campoToken() +
              '<input type="hidden" name="id" value="' + c.id + '">' +
              '<button type="submit" class="btn btn-icon btn-outline-success" title="Dar baixa / Marcar como pago"><i class="bi bi-cash-coin"></i></button>' +
          '</form> '
        : '';
    return '<tr>' +
        '<td><strong>' + escaparHtml(c.conta || '') + '</strong>' + docHtml + '</td>' +
        '<td>' + escaparHtml(c.fornecedorNome || '-') + '</td>' +
        '<td>' + dataBr(c.venc) + '</td>' +
        '<td class="text-end"><strong>R$ ' + moeda(c.valor) + '</strong></td>' +
        '<td class="text-end">R$ ' + moeda(c.pago) + '</td>' +
        '<td>' + badgeSituacao(sit) + '</td>' +
        '<td class="text-center">' + (c.ativo ? 'S' : 'N') + '</td>' +
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
    const idFornecedor = document.getElementById('buscaFornecedor').value;
    const inativos = document.getElementById('buscaInativos').checked;

    const params = new URLSearchParams();
    if (id) params.append('id', id);
    if (conta) params.append('conta', conta);
    if (documento) params.append('documento', documento);
    if (idFornecedor) params.append('idFornecedor', idFornecedor);
    params.append('incluirInativos', inativos);

    tbody.innerHTML = '<tr><td colspan="9" class="text-center text-muted py-3">Buscando...</td></tr>';
    try {
        const resp = await fetch('?handler=BuscarContas&' + params.toString());
        resultadosBusca = await resp.json();

        const contador = document.getElementById('contadorRegistros');
        if (contador) contador.textContent = resultadosBusca.length + ' registros encontrados';

        if (!resultadosBusca.length) {
            tbody.innerHTML = '<tr><td colspan="9" class="text-center text-muted py-4">Nenhuma conta a pagar encontrada</td></tr>';
            return;
        }
        tbody.innerHTML = resultadosBusca.map((c, i) => montarLinha(c, i)).join('');
    } catch (e) {
        tbody.innerHTML = '<tr><td colspan="9" class="text-center text-danger py-4">Erro ao buscar. Tente novamente.</td></tr>';
    }
}

function abrirEdicao(i) {
    mostrarRegistro('modalConta', resultadosBusca[i].registro);
    const c = resultadosBusca[i];
    editarConta(
        c.id, c.conta || '', c.documento || '', c.idFornecedor, (c.idForma ?? ''),
        c.emissao || '', c.venc || '', c.pgto || '', c.valor, c.pago,
        c.status || 'PENDENTE', (c.ativo ? 'true' : 'false'), c.obs || ''
    );
    bootstrap.Modal.getOrCreateInstance(document.getElementById('modalConta')).show();
}

function limparFormConta() {
    document.getElementById('contaTitulo').innerHTML = '<i class="bi bi-cash-stack me-2"></i>Nova Conta a Pagar';
    document.getElementById('contaIdConta').value = '0';
    document.getElementById('contaCodigoDisplay').value = '';
    document.getElementById('contaDsConta').value = '';
    document.getElementById('contaNrDocumento').value = '';
    document.getElementById('contaIdFornecedor').value = '';
    document.getElementById('contaIdFormaPagamento').value = '';
    document.getElementById('contaDtEmissao').value = '';
    document.getElementById('contaDtVencimento').value = '';
    document.getElementById('contaDtPagamento').value = '';
    document.getElementById('contaDsStatus').value = 'PENDENTE';
    document.getElementById('contaVlValor').value = '0';
    document.getElementById('contaVlPago').value = '0';
    definirAtivo('contaFlAtivo', true);
    document.getElementById('contaDsObservacao').value = '';
}

function editarConta(id, conta, doc, idForn, idForma, emissao, venc, pgto, valor, pago, status, ativo, obs) {
    document.getElementById('contaTitulo').innerHTML = '<i class="bi bi-pencil me-2"></i>Editar Conta a Pagar';
    document.getElementById('contaIdConta').value = id;
    document.getElementById('contaCodigoDisplay').value = id;
    document.getElementById('contaDsConta').value = conta;
    document.getElementById('contaNrDocumento').value = doc || '';
    document.getElementById('contaIdFornecedor').value = idForn;
    document.getElementById('contaIdFormaPagamento').value = idForma || '';
    document.getElementById('contaDtEmissao').value = emissao || '';
    document.getElementById('contaDtVencimento').value = venc || '';
    document.getElementById('contaDtPagamento').value = pgto || '';
    document.getElementById('contaDsStatus').value = status || 'PENDENTE';
    document.getElementById('contaVlValor').value = valor;
    document.getElementById('contaVlPago').value = pago;
    definirAtivo('contaFlAtivo', ativo === 'true');
    document.getElementById('contaDsObservacao').value = obs || '';
}

