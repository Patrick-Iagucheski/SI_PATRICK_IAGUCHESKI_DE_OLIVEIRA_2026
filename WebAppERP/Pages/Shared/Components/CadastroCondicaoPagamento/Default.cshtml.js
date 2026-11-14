// Script do componente de cadastro de condicao de pagamento
// (Pages/Shared/Components/CadastroCondicaoPagamento/Default.cshtml).
// Incluido pelo proprio componente, uma vez por pagina. Usado pela tela de
// Condicoes de Pagamento e pela pesquisa de condicao (lupa da condicao no
// cadastro de fornecedor e na Compra).
// Nomes com sufixo "Condicao" para nao colidir com as funcoes das telas.
// Roda antes do bootstrap.bundle (fica no meio do <body>): so usa o
// bootstrap dentro dos eventos, nunca no carregamento.

// ===== Grid de parcelas =====
// Formas ativas (data-formas do modal). Guardadas numa lista para que a forma
// cadastrada ou renomeada pelo + entre tambem nas parcelas adicionadas depois.
let FORMAS_CONDICAO = null;
function formasCondicao() {
    return FORMAS_CONDICAO ??= JSON.parse(document.getElementById('modalCondicao').dataset.formas || '[]');
}

// Forma inativada depois de a condicao ser criada: nao esta na lista (so
// ativas), entao entra so na parcela que ja a usa, marcada "(inativo)", para
// nao sumir. Parcela nova so oferece as ativas; o servidor faz a mesma regra.
function opcoesFormaCondicao(selecionado, nomeSelecionado) {
    let html = '<option value="">Selecione</option>';
    let achou = false;
    formasCondicao().forEach(f => {
        const sel = (String(f.id) === String(selecionado)) ? ' selected' : '';
        if (sel) achou = true;
        html += `<option value="${f.id}"${sel}>${escaparHtml(f.nome)}</option>`;
    });
    if (selecionado && !achou)
        html += `<option value="${escaparHtml(String(selecionado))}" selected>${escaparHtml((nomeSelecionado || 'Forma') + ' (inativo)')}</option>`;
    return html;
}

// Prazo maximo de cada parcela (10 anos), o mesmo do servidor
// (CondicaoPagamentoBusiness.DiasMaximos).
const DIAS_MAXIMOS_CONDICAO = 3650;

// Cada parcela tem um id proprio no select da forma (o + seleciona nele).
let seqFormaCondicao = 0;

function addParcelaCondicao(dias, idForma, perc, formaNome) {
    const tbody = document.querySelector('#condTblParcelas tbody');
    const tr = document.createElement('tr');
    const idSelect = 'condForma' + (++seqFormaCondicao);
    tr.innerHTML =
        '<td><input type="text" class="form-control form-control-sm p-num bg-light" readonly></td>' +
        '<td><input type="number" min="0" max="' + DIAS_MAXIMOS_CONDICAO + '" class="form-control form-control-sm p-dias" value="' + (dias ?? 0) + '"></td>' +
        '<td><select class="form-select form-select-sm p-forma" id="' + idSelect + '"' +
            ' data-pesquisa="abrirPesquisaFormaCondicao(\'' + idSelect + '\')">' +
            opcoesFormaCondicao(idForma ?? '', formaNome) + '</select></td>' +
        '<td><input type="number" step="0.01" min="0" max="100" class="form-control form-control-sm p-perc" value="' + (perc ?? '') + '"></td>' +
        '<td class="text-center"><button type="button" class="btn btn-icon btn-outline-danger" title="Remover" onclick="removeParcelaCondicao(this)"><i class="bi bi-trash"></i></button></td>';
    tbody.appendChild(tr);
    montarCampoSelecao(tr.querySelector('.p-forma'));
    tr.querySelector('.p-perc').addEventListener('input', atualizarTotalCondicao);
    renumerarParcelasCondicao();
    atualizarTotalCondicao();
}

// + da forma na parcela: pesquisa de forma de pagamento (seleciona, edita e
// cadastra pelo Novo). Renomear ou excluir vale para todas as parcelas.
function abrirPesquisaFormaCondicao(idSelect) {
    const parcela = opcoesDoSelectComLista(idSelect, formasCondicao());
    const emTodas = acao => document.querySelectorAll('#condTblParcelas .p-forma').forEach(s => acao(s.id));
    abrirPesquisaForma({
        selecionar: parcela.selecionar,
        atualizado: r => { parcela.atualizado(r); emTodas(id => renomearNoSelect(id, r.id, r.nome)); },
        excluido: r => { parcela.excluido(r); emTodas(id => removerDoSelect(id, r.id)); }
    });
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
    definirAtivo('condFlAtivo', true);
    document.getElementById('condVlJuro').value = '0';
    document.getElementById('condVlMulta').value = '0';
    document.getElementById('condVlDesconto').value = '0';
    document.querySelector('#condTblParcelas tbody').innerHTML = '';
    addParcelaCondicao(0, '', 100);
}

// parcelas: [{ nrDiasVencimento, idFormaPagamento, formaNome, vlPercentual }]
function editarCondicao(id, nome, juro, multa, desc, ativo, parcelas) {
    document.getElementById('condModalTitle').innerHTML = '<i class="bi bi-pencil me-2"></i>Editar Condição de Pagamento';
    document.getElementById('condIdCondicao').value = id;
    document.getElementById('condCodigoDisplay').value = id;
    document.getElementById('condDsCondicao').value = nome;
    definirAtivo('condFlAtivo', ativo === 'true');
    document.getElementById('condVlJuro').value = juro;
    document.getElementById('condVlMulta').value = multa;
    document.getElementById('condVlDesconto').value = desc;
    document.querySelector('#condTblParcelas tbody').innerHTML = '';
    if (!parcelas || parcelas.length === 0) { addParcelaCondicao(0, '', 100); }
    else { parcelas.forEach(p => addParcelaCondicao(p.nrDiasVencimento, p.idFormaPagamento, p.vlPercentual, p.formaNome)); }
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
    if (parcelas.some(p => p.nrDiasVencimento > DIAS_MAXIMOS_CONDICAO)) {
        e.preventDefault(); exibirErro(this, 'Os dias para vencimento devem ser no máximo ' + DIAS_MAXIMOS_CONDICAO + ' (10 anos).'); return;
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
});

// ===== Pesquisa de condicao (a lupa do campo de condicao de outras telas) =====
// opcoes: o que a tela faz com o registro (ver abrirPesquisaComCadastro no module.js).
function urlCondicoes(handler) {
    return document.getElementById('modalCondicao').dataset.urlCondicoes + '?handler=' + handler;
}

// Registro da pesquisa / busca (BuscarCondicoes) no formulario de edicao.
function editarCondicaoPorRegistro(c) {
    editarCondicao(c.id, c.nome || '', c.juro, c.multa, c.desconto, c.ativo ? 'true' : 'false', c.parcelas || []);
}

const PESQUISA_CONDICAO = {
    chave: 'condicao',
    titulo: 'Pesquisar Condição de Pagamento',
    url: () => urlCondicoes('BuscarCondicoes'),
    inativos: 'Exibir Condições Inativas',
    filtros: [
        { param: 'id', rotulo: 'ID', tipo: 'number', col: 2 },
        { param: 'condicao', rotulo: 'Condição', col: 6 }
    ],
    colunas: ['Código', 'Condição', 'Parcelas', 'Juro (%)', 'Multa (%)', 'Desconto (%)'],
    celulas: c => ['<strong>' + c.id + '</strong>', escaparHtml(c.nome), (c.parcelas || []).length,
                   moeda(c.juro), moeda(c.multa), moeda(c.desconto)],
    modal: 'modalCondicao',
    evento: 'condicao:salva',
    preencher: editarCondicaoPorRegistro,
    limpar: limparFormCondicao,
    urlExcluir: () => urlCondicoes('Excluir'),
    nome: c => c.nome
};

function abrirPesquisaCondicao(opcoes) {
    abrirPesquisaComCadastro(PESQUISA_CONDICAO, opcoes);
}
