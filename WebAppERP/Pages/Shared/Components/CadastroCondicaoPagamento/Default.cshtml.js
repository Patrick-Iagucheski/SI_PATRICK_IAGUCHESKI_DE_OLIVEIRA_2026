// Script do componente de cadastro de condicao de pagamento
// (Pages/Shared/Components/CadastroCondicaoPagamento/Default.cshtml).
// Incluido pelo proprio componente, uma vez por pagina. Usado pela tela de
// Condicoes de Pagamento e pelo + do cadastro de fornecedor.
// Nomes com sufixo "Condicao" para nao colidir com as funcoes das telas.
// Roda antes do bootstrap.bundle (fica no meio do <body>): so usa o
// bootstrap dentro dos eventos, nunca no carregamento.

// ===== Grid de parcelas =====
function opcoesFormaCondicao(selecionado) {
    const formas = JSON.parse(document.getElementById('modalCondicao').dataset.formas || '[]');
    let html = '<option value="">Selecione</option>';
    formas.forEach(f => {
        const sel = (String(f.id) === String(selecionado)) ? ' selected' : '';
        html += `<option value="${f.id}"${sel}>${escaparHtml(f.nome)}</option>`;
    });
    return html;
}

function addParcelaCondicao(dias, idForma, perc) {
    const tbody = document.querySelector('#condTblParcelas tbody');
    const tr = document.createElement('tr');
    tr.innerHTML =
        '<td><input type="text" class="form-control form-control-sm p-num bg-light" readonly></td>' +
        '<td><input type="number" min="0" class="form-control form-control-sm p-dias" value="' + (dias ?? 0) + '"></td>' +
        '<td><select class="form-select form-select-sm p-forma">' + opcoesFormaCondicao(idForma ?? '') + '</select></td>' +
        '<td><input type="number" step="0.01" min="0" max="100" class="form-control form-control-sm p-perc" value="' + (perc ?? '') + '"></td>' +
        '<td class="text-center"><button type="button" class="btn btn-icon btn-outline-danger" title="Remover" onclick="removeParcelaCondicao(this)"><i class="bi bi-trash"></i></button></td>';
    tbody.appendChild(tr);
    tr.querySelector('.p-perc').addEventListener('input', atualizarTotalCondicao);
    renumerarParcelasCondicao();
    atualizarTotalCondicao();
}

function removeParcelaCondicao(btn) {
    btn.closest('tr').remove();
    renumerarParcelasCondicao();
    atualizarTotalCondicao();
}

function renumerarParcelasCondicao() {
    document.querySelectorAll('#condTblParcelas tbody tr').forEach((tr, i) => {
        tr.querySelector('.p-num').value = i + 1;
    });
}

function atualizarTotalCondicao() {
    let soma = 0;
    document.querySelectorAll('#condTblParcelas tbody .p-perc').forEach(inp => {
        soma += parseFloat(inp.value) || 0;
    });
    const el = document.getElementById('condTotalPercentual');
    el.textContent = soma.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + ' %';
    el.classList.toggle('text-danger', Math.abs(soma - 100) > 0.01);
    el.classList.toggle('text-success', Math.abs(soma - 100) <= 0.01);
}

function coletarParcelasCondicao() {
    const parcelas = [];
    document.querySelectorAll('#condTblParcelas tbody tr').forEach((tr, i) => {
        parcelas.push({
            nrParcela: i + 1,
            nrDiasVencimento: parseInt(tr.querySelector('.p-dias').value) || 0,
            idFormaPagamento: parseInt(tr.querySelector('.p-forma').value) || 0,
            vlPercentual: parseFloat(tr.querySelector('.p-perc').value) || 0
        });
    });
    return parcelas;
}

// ===== Abertura do modal =====
function limparFormCondicao() {
    document.getElementById('condModalTitle').innerHTML = '<i class="bi bi-list-check me-2"></i>Nova Condição de Pagamento';
    document.getElementById('condIdCondicao').value = '0';
    document.getElementById('condCodigoDisplay').value = '';
    document.getElementById('condDsCondicao').value = '';
    document.getElementById('condFlAtivo').value = 'true';
    document.getElementById('condVlJuro').value = '0';
    document.getElementById('condVlMulta').value = '0';
    document.getElementById('condVlDesconto').value = '0';
    document.querySelector('#condTblParcelas tbody').innerHTML = '';
    addParcelaCondicao(0, '', 100);
}

// parcelas: [{ nrDiasVencimento, idFormaPagamento, vlPercentual }]
function editarCondicao(id, nome, juro, multa, desc, ativo, parcelas) {
    document.getElementById('condModalTitle').innerHTML = '<i class="bi bi-pencil me-2"></i>Editar Condição de Pagamento';
    document.getElementById('condIdCondicao').value = id;
    document.getElementById('condCodigoDisplay').value = id;
    document.getElementById('condDsCondicao').value = nome;
    document.getElementById('condFlAtivo').value = ativo;
    document.getElementById('condVlJuro').value = juro;
    document.getElementById('condVlMulta').value = multa;
    document.getElementById('condVlDesconto').value = desc;
    document.querySelector('#condTblParcelas tbody').innerHTML = '';
    if (!parcelas || parcelas.length === 0) { addParcelaCondicao(0, '', 100); }
    else { parcelas.forEach(p => addParcelaCondicao(p.nrDiasVencimento, p.idFormaPagamento, p.vlPercentual)); }
}

// ===== Submit: serializa parcelas e valida =====
document.getElementById('condForm')?.addEventListener('submit', function (e) {
    const parcelas = coletarParcelasCondicao();
    if (parcelas.length === 0) {
        e.preventDefault(); exibirErro(this, 'Adicione ao menos uma parcela.'); return;
    }
    if (parcelas.some(p => !p.idFormaPagamento)) {
        e.preventDefault(); exibirErro(this, 'Selecione a forma de pagamento em todas as parcelas.'); return;
    }
    if (parcelas.some(p => p.vlPercentual <= 0 || p.vlPercentual > 100)) {
        e.preventDefault(); exibirErro(this, 'O percentual de cada parcela deve estar entre 0 e 100.'); return;
    }
    const soma = parcelas.reduce((s, p) => s + p.vlPercentual, 0);
    if (Math.abs(soma - 100) > 0.01) {
        e.preventDefault();
        exibirErro(this, 'O percentual total das parcelas deve ser 100%. Atual: ' + soma.toFixed(2) + '%.');
        return;
    }
    document.getElementById('condParcelasJson').value = JSON.stringify(parcelas);

    // Modo AJAX: grava sem recarregar a tela que abriu o cadastro.
    if (document.getElementById('modalCondicao').dataset.modo === 'ajax') {
        e.preventDefault();
        salvarCondicaoAjax(this);
    }
});

// Grava a condicao e avisa a tela de origem pelo evento 'condicao:salva'.
async function salvarCondicaoAjax(form) {
    const btn = form.querySelector('button[type="submit"]');
    exibirErro(form, null);
    btn.disabled = true;
    try {
        const url = document.getElementById('modalCondicao').dataset.urlCondicoes + '?handler=SalvarAjax';
        const resp = await fetch(url, { method: 'POST', body: new FormData(form) });
        const data = await resp.json();
        if (!data.sucesso) { exibirErro(form, data.mensagem); return; }
        bootstrap.Modal.getInstance(document.getElementById('modalCondicao')).hide();
        document.dispatchEvent(new CustomEvent('condicao:salva', { detail: data }));
    } catch (e) {
        exibirErro(form, 'Erro ao salvar a condição de pagamento. Tente novamente.');
    } finally { btn.disabled = false; }
}
