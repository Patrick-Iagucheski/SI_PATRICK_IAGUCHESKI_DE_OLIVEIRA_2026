// Script da tela de Compra (inclusao e edicao) (Pages/Compras/Compra.cshtml).
// Os valores que vem do servidor estao no bloco JSON #dadosTela da pagina.
const DADOS_TELA = JSON.parse(document.getElementById('dadosTela').textContent);

// Tipos de item (constantes da entidade no servidor).
const TIPO_PRODUTO = DADOS_TELA.TIPO_ITEM.PRODUTO;
const TIPO_SERVICO = DADOS_TELA.TIPO_ITEM.SERVICO;

const FORMAS = DADOS_TELA.FORMAS;
const CONDICOES = DADOS_TELA.CONDICOES;
const SOMENTE_LEITURA = DADOS_TELA.SOMENTE_LEITURA;
const CAMPOS_CHAVE = ['NrModelo', 'NrSerie', 'NrNota', 'IdFornecedor'];
const CONDICAO_FORNECEDOR = DADOS_TELA.CONDICAO_FORNECEDOR;

// Enquanto a chave nao for validada, nenhum outro campo e editavel.
let chaveValidada = DADOS_TELA.chaveValidada;

let ITENS = DADOS_TELA.ITENS;
let PAGAMENTOS = DADOS_TELA.PAGAMENTOS;

// Indice em edicao (null = incluindo um novo).
let itemEditandoIdx = null;
let pagamentoEditandoIdx = null;

// Contexto do modal de pesquisa: 'fornecedor' | 'transportador' | 'item'.
let buscaContexto = null;

// ---------------- Utilitarios ----------------

function qtd(n) {
    return (Number(n) || 0).toLocaleString('pt-BR', { minimumFractionDigits: 3, maximumFractionDigits: 4 });
}

function arred2(n) {
    return Math.round(((Number(n) || 0) + Number.EPSILON) * 100) / 100;
}

function num(id) {
    const el = document.getElementById(id);
    if (!el) return 0;
    return parseFloat(String(el.value).replace(',', '.')) || 0;
}

function texto(id) {
    const el = document.getElementById(id);
    return el ? el.value.trim() : '';
}

function nomeForma(id) {
    const f = FORMAS.find(x => x.id === id);
    return f ? f.nome : '-';
}

function nomeCondicao(id) {
    const c = CONDICOES.find(x => x.id === id);
    return c ? c.nome : '-';
}

// ---------------- Pesquisa (fornecedor / transportadora / item) ----------------
// Um modal so para todas as pesquisas da tela. Cada pesquisa declara:
//   titulo, url (handler de busca), filtros (campos com o nome do parametro),
//   colunas e celulas da grade, selecionar (o que vai para a compra) e,
//   quando houver cadastro, editar (abre o componente ja preenchido).
// A busca roda no botao Buscar ou no Enter, como nas telas de cadastro, e ao
// abrir ja lista os registros disponiveis.

const PESQUISAS = {
    fornecedor: {
        titulo: 'Pesquisar Fornecedor',
        // Mesma busca da tela de Fornecedores (so ativos, sem diferenciar acento).
        url: () => DADOS_TELA.URL_FORNECEDORES + '?handler=BuscarFornecedores',
        filtros: [
            { param: 'id', rotulo: 'ID', tipo: 'number', col: 2 },
            { param: 'razao', rotulo: 'Razão Social', col: 4 },
            { param: 'fantasia', rotulo: 'Nome Fantasia', col: 3 },
            { param: 'documento', rotulo: 'CPF / CNPJ', col: 3 }
        ],
        colunas: ['Código', 'Razão Social', 'Nome Fantasia', 'CPF / CNPJ', 'Cidade'],
        celulas: f => ['<strong>' + f.id + '</strong>', escaparHtml(f.razaoSocial), escaparHtml(f.nomeFantasia || '-'),
                       documentoFornecedor(f), escaparHtml(f.cidadeNome || '-')],
        selecionar: f => ({ id: f.id, nome: f.razaoSocial }),
        editar: document.getElementById('modalFornecedor') ? f => { editarFornecedor(f); abrirModalEmpilhado('modalFornecedor'); } : null
    },
    transportador: {
        titulo: 'Pesquisar Transportadora',
        url: () => '?handler=BuscarTransportador',
        filtros: [{ param: 'termo', rotulo: 'Razão Social', col: 6 }],
        colunas: ['Código', 'Razão Social'],
        celulas: r => ['<strong>' + r.id + '</strong>', escaparHtml(r.nome)],
        selecionar: r => r
    },
    item: {
        titulo: () => document.getElementById('itTipo').value === TIPO_PRODUTO ? 'Pesquisar Produto' : 'Pesquisar Serviço',
        url: () => '?handler=' + (document.getElementById('itTipo').value === TIPO_PRODUTO ? 'BuscarProduto' : 'BuscarServico'),
        filtros: [{ param: 'termo', rotulo: 'Descrição', col: 6 }],
        colunas: ['Código', 'Descrição'],
        celulas: r => ['<strong>' + r.id + '</strong>', escaparHtml(r.nome)],
        selecionar: r => r
    }
};

const pesquisaAtual = () => PESQUISAS[buscaContexto];

// CPF, CNPJ ou documento conforme o tipo de pessoa do fornecedor.
function documentoFornecedor(f) {
    if (f.tipoPessoa === 'F') return f.cpf ? mascaraCPF(f.cpf) : '-';
    if (f.tipoPessoa === 'E' || f.tipoPessoa === 'O') return escaparHtml(f.documento || '-');
    return f.cnpj ? mascaraCNPJ(f.cnpj) : '-';
}

function abrirModalBusca(contexto) {
    buscaContexto = contexto;
    const p = pesquisaAtual();
    document.getElementById('buscaTitulo').textContent = typeof p.titulo === 'function' ? p.titulo() : p.titulo;

    // Filtros da pesquisa (Enter em qualquer um busca).
    const filtros = document.getElementById('buscaFiltros');
    filtros.innerHTML = p.filtros.map(f =>
        '<div class="col-md-' + f.col + '">' +
            '<label class="form-label small mb-1">' + f.rotulo + '</label>' +
            '<input type="' + (f.tipo || 'text') + '" class="form-control" data-param="' + f.param + '" autocomplete="off"' +
            (f.tipo === 'number' ? ' min="1"' : '') + ' />' +
        '</div>').join('');
    filtros.querySelectorAll('input').forEach(i => i.addEventListener('keydown', e => {
        if (e.key === 'Enter') { e.preventDefault(); executarBusca(); }
    }));

    document.getElementById('buscaCabecalho').innerHTML =
        p.colunas.map((c, i) => '<th' + (i === 0 ? ' style="width: 90px;"' : '') + '>' + c + '</th>').join('') +
        '<th class="text-center" style="width: 120px;">Ações</th>';

    // "+ Novo" so onde ha componente de cadastro: fornecedor e produto.
    const novo = cadastroDaBusca();
    const btnNovo = document.getElementById('btnBuscaNovo');
    btnNovo.classList.toggle('d-none', !novo);
    if (novo) btnNovo.title = novo.titulo;

    bootstrap.Modal.getOrCreateInstance(document.getElementById('modalBusca')).show();
    executarBusca();   // ja abre com os registros disponiveis
}

function abrirBuscaFornecedor() { abrirModalBusca('fornecedor'); }
function abrirBuscaTransportador() { abrirModalBusca('transportador'); }
function abrirBuscaItem() { abrirModalBusca('item'); }

async function executarBusca() {
    const p = pesquisaAtual();
    const colspan = p.colunas.length + 1;
    const body = document.getElementById('buscaBody');

    const params = new URLSearchParams();
    document.querySelectorAll('#buscaFiltros [data-param]').forEach(i => {
        if (i.value.trim()) params.append(i.dataset.param, i.value.trim());
    });

    body.innerHTML = '<tr><td colspan="' + colspan + '" class="text-center text-muted py-3">Buscando...</td></tr>';
    try {
        const resp = await fetch(p.url() + '&' + params.toString());
        const lista = await resp.json();

        if (!lista.length) {
            body.innerHTML = '<tr><td colspan="' + colspan + '" class="text-center text-muted py-3">Nenhum registro encontrado</td></tr>';
            return;
        }

        body.innerHTML = '';
        lista.forEach(reg => {
            const tr = document.createElement('tr');
            tr.innerHTML = p.celulas(reg).map(c => '<td>' + c + '</td>').join('') +
                '<td class="text-center text-nowrap">' +
                    (p.editar ? '<button type="button" class="btn btn-icon btn-outline-primary me-1" data-acao="editar" title="Editar"><i class="bi bi-pencil"></i></button>' : '') +
                    '<button type="button" class="btn btn-icon btn-outline-success" data-acao="selecionar" title="Selecionar"><i class="bi bi-check-lg"></i></button>' +
                '</td>';
            tr.querySelector('[data-acao="selecionar"]').addEventListener('click', () => selecionarBusca(p.selecionar(reg)));
            tr.querySelector('[data-acao="editar"]')?.addEventListener('click', () => { edicaoNaPesquisa = true; p.editar(reg); });
            body.appendChild(tr);
        });
    } catch {
        body.innerHTML = '<tr><td colspan="' + colspan + '" class="text-center text-danger py-3">Falha ao pesquisar</td></tr>';
    }
}

function selecionarBusca(reg) {
    if (buscaContexto === 'fornecedor') {
        document.getElementById('IdFornecedor').value = reg.id;
        document.getElementById('nomeFornecedor').value = reg.nome;
    } else if (buscaContexto === 'transportador') {
        document.getElementById('IdTransportador').value = reg.id;
        document.getElementById('nomeTransportador').value = reg.nome;
    } else {
        aplicarItemSelecionado(reg);
    }
    bootstrap.Modal.getOrCreateInstance(document.getElementById('modalBusca')).hide();
}

// Componente de cadastro que o "+ Novo" abre na pesquisa atual, ou null
// quando a pesquisa nao tem cadastro (transportadora, servico) ou o
// componente nao foi renderizado (tela somente leitura / chave travada).
function cadastroDaBusca() {
    if (buscaContexto === 'fornecedor' && document.getElementById('modalFornecedor'))
        return { modal: 'modalFornecedor', limpar: limparForm, titulo: 'Cadastrar novo fornecedor' };
    if (buscaContexto === 'item' && document.getElementById('itTipo').value === TIPO_PRODUTO
        && document.getElementById('modalProduto'))
        return { modal: 'modalProduto', limpar: limparFormProduto, titulo: 'Cadastrar novo produto' };
    return null;
}

// "+ Novo" da pesquisa: abre o cadastro vazio por cima da pesquisa.
function novoCadastroBusca() {
    const novo = cadastroDaBusca();
    if (!novo) return;
    edicaoNaPesquisa = false;
    novo.limpar();
    abrirModalEmpilhado(novo.modal);
}

// Registro gravado pelo componente. Cadastro novo ja volta selecionado na
// compra; edicao feita pela pesquisa volta para a lista atualizada (e, se for
// o fornecedor desta compra, atualiza o nome exibido).
let edicaoNaPesquisa = false;
document.addEventListener('fornecedor:salvo', e => {
    if (edicaoNaPesquisa) {
        edicaoNaPesquisa = false;
        if (String(e.detail.id) === document.getElementById('IdFornecedor').value)
            document.getElementById('nomeFornecedor').value = e.detail.razaoSocial;
        executarBusca();
        return;
    }
    selecionarBusca({ id: e.detail.id, nome: e.detail.razaoSocial });
});
document.addEventListener('produto:salvo', e =>
    selecionarBusca({ id: e.detail.id, nome: e.detail.nome, unidade: e.detail.unidade, custo: e.detail.custo }));

// Preenche os campos do item a partir do produto/servico escolhido.
function aplicarItemSelecionado(reg) {
    document.getElementById('itCodigo').value = reg.id;
    document.getElementById('itDescricao').textContent = reg.nome;
    if (reg.unidade) document.getElementById('itUnidade').value = reg.unidade;
    const preco = reg.custo ?? reg.valor;
    if (preco) document.getElementById('itVlUnit').value = preco;
    calcItemTotal();
}

// Digitou o ID direto no campo: busca o registro para exibir a descricao.
async function onCodigoItemChange() {
    const id = parseInt(document.getElementById('itCodigo').value) || 0;
    if (!id) { document.getElementById('itDescricao').textContent = ''; return; }

    const tipo = document.getElementById('itTipo').value;
    const handler = tipo === TIPO_PRODUTO ? 'BuscarProduto' : 'BuscarServico';

    try {
        const resp = await fetch('?handler=' + handler + '&id=' + id);
        const lista = await resp.json();
        if (lista.length) aplicarItemSelecionado(lista[0]);
        else document.getElementById('itDescricao').textContent = 'Não encontrado';
    } catch {
        document.getElementById('itDescricao').textContent = '';
    }
}

// Mesma ideia para fornecedor e transportadora.
async function buscarPorId(handler, idInput, nomeInput) {
    const id = parseInt(document.getElementById(idInput).value) || 0;
    if (!id) { document.getElementById(nomeInput).value = ''; return; }

    try {
        const resp = await fetch('?handler=' + handler + '&id=' + id);
        const lista = await resp.json();
        document.getElementById(nomeInput).value = lista.length ? lista[0].nome : 'Não encontrado';
    } catch {
        document.getElementById(nomeInput).value = '';
    }
}

// ---------------- Itens ----------------

function onTipoChange() {
    document.getElementById('itCodigo').value = '';
    document.getElementById('itDescricao').textContent = '';
    document.getElementById('itVlUnit').value = '0';
    calcItemTotal();
}

// Qual dos dois campos de desconto o usuario digitou por ultimo: o
// outro e recalculado a partir dele (inclusive quando muda qtd/preco).
let descontoPorPercentual = false;

function calcItemTotal() {
    const bruto = arred2(num('itQt') * num('itVlUnit'));

    if (descontoPorPercentual) {
        document.getElementById('itDesc').value = arred2(bruto * num('itDescPerc') / 100).toFixed(2);
    } else {
        document.getElementById('itDescPerc').value =
            bruto > 0 ? arred2(num('itDesc') / bruto * 100).toFixed(2) : '0';
    }

    const total = bruto - num('itDesc');
    document.getElementById('itTotal').value = (total > 0 ? total : 0).toFixed(2);
}

function adicionarItem() {
    const tipo = document.getElementById('itTipo').value;
    const codigo = parseInt(texto('itCodigo')) || 0;
    const descricao = document.getElementById('itDescricao').textContent;

    if (!codigo || !descricao || descricao === 'Não encontrado') {
        exibirErro(secaoDoCampo('itTipo'), 'Selecione um ' + (tipo === TIPO_PRODUTO ? 'produto' : 'serviço') + ' válido.');
        return;
    }

    const qt = num('itQt');
    if (qt <= 0) { exibirErro(secaoDoCampo('itTipo'), 'A quantidade deve ser maior que zero.'); return; }

    const vlUnit = num('itVlUnit');
    const desc = num('itDesc');
    const bruto = arred2(qt * vlUnit);

    if (desc > bruto) {
        exibirErro(secaoDoCampo('itTipo'), 'O desconto (R$ ' + moeda(desc) + ') é maior que o valor do item (R$ ' + moeda(bruto) + ').');
        return;
    }

    const item = {
        tpItem: tipo,
        idProduto: tipo === TIPO_PRODUTO ? codigo : null,
        idServico: tipo === TIPO_SERVICO ? codigo : null,
        codigo: codigo,
        descricao: descricao,
        // A classificacao saiu da tela. Item antigo que ja tinha uma
        // a mantem ao ser editado, para nao perder o que foi gravado.
        idClassificacaoConta: itemEditandoIdx !== null ? (ITENS[itemEditandoIdx].idClassificacaoConta ?? null) : null,
        sgUnidade: document.getElementById('itUnidade').value || null,
        qtItem: qt,
        vlUnitario: vlUnit,
        vlDesconto: desc,
        vlTotal: bruto - desc
    };

    if (itemEditandoIdx !== null) ITENS[itemEditandoIdx] = item;
    else ITENS.push(item);

    limparItemForm();
    renderItens();
}

function limparItemForm() {
    itemEditandoIdx = null;
    descontoPorPercentual = false;
    document.getElementById('itCodigo').value = '';
    document.getElementById('itDescricao').textContent = '';
    document.getElementById('itUnidade').value = '';
    document.getElementById('itQt').value = '1';
    document.getElementById('itVlUnit').value = '0';
    document.getElementById('itDesc').value = '0';
    document.getElementById('itDescPerc').value = '0';
    document.getElementById('itTotal').value = '';
    document.getElementById('itEditando').textContent = '';
}

function editarItem(idx) {
    const it = ITENS[idx];
    itemEditandoIdx = idx;
    descontoPorPercentual = false;

    document.getElementById('itTipo').value = it.tpItem;
    document.getElementById('itCodigo').value = it.codigo;
    document.getElementById('itDescricao').textContent = it.descricao;
    document.getElementById('itUnidade').value = it.sgUnidade || '';
    document.getElementById('itQt').value = it.qtItem;
    document.getElementById('itVlUnit').value = it.vlUnitario;
    document.getElementById('itDesc').value = it.vlDesconto;

    document.getElementById('itEditando').textContent = 'Editando o item ' + (idx + 1) + '.';

    calcItemTotal();
    document.getElementById('itQt').focus();
}

function removerItem(idx) {
    if (!confirm('Remover este item da compra?')) return;
    ITENS.splice(idx, 1);
    limparItemForm();
    renderItens();
}

function renderItens() {
    const tb = document.getElementById('itensBody');
    tb.innerHTML = '';

    if (!ITENS.length) {
        tb.innerHTML = '<tr><td colspan="10" class="text-center text-muted py-3">Nenhum produto/serviço adicionado</td></tr>';
    }

    const rateios = calcRateio();

    ITENS.forEach((it, idx) => {
        const badge = it.tpItem === TIPO_PRODUTO ? 'badge-agendado' : 'badge-confirmado';
        const acoes = SOMENTE_LEITURA ? '<td></td>' :
            '<td class="text-center" style="white-space:nowrap;">' +
            '<button type="button" class="btn btn-icon btn-outline-primary" title="Editar" onclick="editarItem(' + idx + ')"><i class="bi bi-pencil"></i></button> ' +
            '<button type="button" class="btn btn-icon btn-outline-danger" title="Excluir" onclick="removerItem(' + idx + ')"><i class="bi bi-trash"></i></button></td>';

        const bruto = arred2(it.qtItem * it.vlUnitario);
        const percDesc = bruto > 0 && it.vlDesconto > 0
            ? '<div class="text-muted small">' + moeda(it.vlDesconto / bruto * 100) + '%</div>' : '';
        // 2 casas, arredondando como o servidor (5 para cima, abaixo de 5 para baixo).
        const custoUnit = it.qtItem > 0 ? arred2(((Number(it.vlTotal) || 0) + rateios[idx]) / it.qtItem) : 0;

        const tr = document.createElement('tr');
        tr.innerHTML =
            '<td>' + (idx + 1) + '</td>' +
            '<td><strong>' + escaparHtml(it.descricao) + '</strong>' +
                '<div class="text-muted small"><span class="badge-status ' + badge + '">' + it.tpItem + '</span> Cód. ' + it.codigo + '</div></td>' +
            '<td class="text-center">' + escaparHtml(it.sgUnidade || '-') + '</td>' +
            '<td class="text-end">' + qtd(it.qtItem) + '</td>' +
            '<td class="text-end">R$ ' + moeda(it.vlUnitario) + '</td>' +
            '<td class="text-end">R$ ' + moeda(it.vlDesconto) + percDesc + '</td>' +
            '<td class="text-end"><strong>R$ ' + moeda(it.vlTotal) + '</strong></td>' +
            '<td class="text-end">' + (it.tpItem === TIPO_PRODUTO ? 'R$ ' + moeda(rateios[idx]) : '-') + '</td>' +
            '<td class="text-end">R$ ' + moeda(custoUnit) + '</td>' +
            acoes;
        tb.appendChild(tr);
    });

    // Totalizador da grade.
    const soma = campo => ITENS.reduce((a, b) => a + (Number(b[campo]) || 0), 0);
    document.getElementById('itensFoot').style.display = ITENS.length ? '' : 'none';
    document.getElementById('totQtd').textContent = qtd(soma('qtItem'));
    document.getElementById('totDesconto').textContent = 'R$ ' + moeda(soma('vlDesconto'));
    document.getElementById('totItens').textContent = 'R$ ' + moeda(soma('vlTotal'));
    document.getElementById('totRateio').textContent = 'R$ ' + moeda(rateios.reduce((a, b) => a + b, 0));

    document.getElementById('contadorItens').textContent =
        ITENS.length + (ITENS.length === 1 ? ' item' : ' itens');

    calcTotais();
    agendarAutoSalvar();
}

// Rateio do custo adicional (frete + seguro + outros) entre os PRODUTOS,
// proporcional ao total de cada um. O ultimo produto absorve a diferenca
// de arredondamento. Mesma conta de RatearCustoAdicional no servidor.
function calcRateio() {
    const custo = custoAdicional();
    const produtos = ITENS.map((it, i) => it.tpItem === TIPO_PRODUTO ? i : -1).filter(i => i >= 0);
    const base = produtos.reduce((a, i) => a + (Number(ITENS[i].vlTotal) || 0), 0);
    const rateios = ITENS.map(() => 0);
    let acumulado = 0;

    produtos.forEach((i, n) => {
        const proporcao = base > 0 ? (Number(ITENS[i].vlTotal) || 0) / base : 1 / produtos.length;
        const valor = n === produtos.length - 1 ? arred2(custo - acumulado) : arred2(custo * proporcao);
        acumulado += valor;
        rateios[i] = valor;
    });

    return rateios;
}

// ---------------- Totais ----------------

function totalProdutos() {
    return ITENS.reduce((a, b) => a + (Number(b.vlTotal) || 0), 0);
}

function custoAdicional() {
    return num('VlFrete') + num('VlSeguro') + num('VlOutrasDespesas');
}

function totalCompra() {
    return totalProdutos() + custoAdicional();
}

function calcTotais() {
    document.getElementById('lblTotalProdutos').value = 'R$ ' + moeda(totalProdutos());
    document.getElementById('lblCustoAdicional').value = 'R$ ' + moeda(custoAdicional());
    document.getElementById('lblTotalCompra').textContent = 'R$ ' + moeda(totalCompra());
    calcTotalParcelas();
}

function calcTotalParcelas() {
    const soma = PAGAMENTOS.reduce((a, b) => a + (Number(b.vlParcela) || 0), 0);
    const diferenca = totalCompra() - soma;

    document.getElementById('lblTotalParcelas').textContent = 'R$ ' + moeda(soma);

    const lbl = document.getElementById('lblDiferencaParcelas');
    if (!PAGAMENTOS.length) {
        lbl.innerHTML = '';
    } else if (Math.abs(diferenca) <= 0.01) {
        lbl.innerHTML = '<span class="badge-status badge-concluido">Confere com o total</span>';
    } else {
        lbl.innerHTML = '<span class="badge-status badge-cancelado">Diferença de R$ ' + moeda(diferenca) + '</span>';
    }
}

// ---------------- Pagamentos ----------------

function adicionarPagamento() {
    const idForma = parseInt(document.getElementById('pgForma').value) || 0;
    if (!idForma) { exibirErro(secaoDoCampo('pgValor'), 'Selecione a forma de pagamento.'); return; }

    const valor = num('pgValor');
    if (valor <= 0) { exibirErro(secaoDoCampo('pgValor'), 'Informe o valor da parcela.'); return; }

    const venc = texto('pgVencimento');
    if (!venc) { exibirErro(secaoDoCampo('pgValor'), 'Informe a data de vencimento.'); return; }

    const idCondicao = parseInt(document.getElementById('IdCondicaoPagamento').value) || null;

    const parcela = {
        idCondicaoPagamento: idCondicao,
        condicaoNome: nomeCondicao(idCondicao),
        idFormaPagamento: idForma,
        formaNome: nomeForma(idForma),
        vlParcela: valor,
        dtVencimento: venc
    };

    if (pagamentoEditandoIdx !== null) PAGAMENTOS[pagamentoEditandoIdx] = parcela;
    else PAGAMENTOS.push(parcela);

    limparPagamentoForm();
    renderPagamentos();
}

function limparPagamentoForm() {
    pagamentoEditandoIdx = null;
    document.getElementById('pgForma').value = '';
    document.getElementById('pgValor').value = '0';
    document.getElementById('pgVencimento').value = '';
    document.getElementById('pgEditando').textContent = '';
}

function editarPagamento(idx) {
    const p = PAGAMENTOS[idx];
    pagamentoEditandoIdx = idx;

    const selCondicao = document.getElementById('IdCondicaoPagamento');
    if (p.idCondicaoPagamento && !selCondicao.disabled) selCondicao.value = p.idCondicaoPagamento;
    document.getElementById('pgForma').value = p.idFormaPagamento;
    document.getElementById('pgValor').value = p.vlParcela;
    document.getElementById('pgVencimento').value = p.dtVencimento;
    document.getElementById('pgEditando').textContent = 'Editando a parcela ' + (idx + 1) + '.';

    document.getElementById('pgValor').focus();
}

function removerPagamento(idx) {
    if (!confirm('Remover esta parcela?')) return;
    PAGAMENTOS.splice(idx, 1);
    limparPagamentoForm();
    renderPagamentos();
}

function renderPagamentos() {
    const tb = document.getElementById('pagamentosBody');
    tb.innerHTML = '';

    if (!PAGAMENTOS.length) {
        tb.innerHTML = '<tr><td colspan="6" class="text-center text-muted py-3">Nenhuma parcela adicionada</td></tr>';
    }

    PAGAMENTOS.forEach((p, idx) => {
        const acoes = SOMENTE_LEITURA ? '<td></td>' :
            '<td class="text-center" style="white-space:nowrap;">' +
            '<button type="button" class="btn btn-icon btn-outline-primary" title="Editar" onclick="editarPagamento(' + idx + ')"><i class="bi bi-pencil"></i></button> ' +
            '<button type="button" class="btn btn-icon btn-outline-danger" title="Excluir" onclick="removerPagamento(' + idx + ')"><i class="bi bi-trash"></i></button></td>';

        const partes = p.dtVencimento.split('-');
        const vencBr = partes.length === 3 ? partes[2] + '/' + partes[1] + '/' + partes[0] : p.dtVencimento;

        const tr = document.createElement('tr');
        tr.innerHTML =
            '<td>' + (idx + 1) + '</td>' +
            '<td>' + escaparHtml(p.condicaoNome || nomeCondicao(p.idCondicaoPagamento)) + '</td>' +
            '<td>' + escaparHtml(p.formaNome || nomeForma(p.idFormaPagamento)) + '</td>' +
            '<td class="text-end"><strong>R$ ' + moeda(p.vlParcela) + '</strong></td>' +
            '<td>' + vencBr + '</td>' +
            acoes;
        tb.appendChild(tr);
    });

    calcTotalParcelas();
    agendarAutoSalvar();
}

// Gera as parcelas a partir da condicao de pagamento cadastrada
// (dias de vencimento, percentual e forma de cada parcela).
async function gerarParcelasDaCondicao() {
    const idCondicao = parseInt(document.getElementById('IdCondicaoPagamento').value) || 0;
    if (!idCondicao) { exibirErro(secaoDoCampo('IdCondicaoPagamento'), 'Selecione a condição de pagamento.'); return; }

    const total = totalCompra();
    if (total <= 0) { exibirErro(secaoDoCampo('IdCondicaoPagamento'), 'Informe os itens da compra antes de gerar as parcelas.'); return; }

    const base = texto('DtEmissao');
    if (!base) { exibirErro(secaoDoCampo('IdCondicaoPagamento'), 'Informe a data de emissão.'); return; }

    let modelo;
    try {
        const resp = await fetch('?handler=BuscarParcelasCondicao&idCondicao=' + idCondicao);
        modelo = await resp.json();
    } catch {
        exibirErro(secaoDoCampo('IdCondicaoPagamento'), 'Não foi possível carregar as parcelas da condição de pagamento.');
        return;
    }

    if (!modelo.length) { exibirErro(secaoDoCampo('IdCondicaoPagamento'), 'Esta condição de pagamento não possui parcelas cadastradas.'); return; }
    if (PAGAMENTOS.length && !confirm('As parcelas atuais serão substituídas. Continuar?')) return;

    PAGAMENTOS = [];
    let acumulado = 0;

    modelo.forEach((m, i) => {
        const venc = new Date(base + 'T00:00:00');
        venc.setDate(venc.getDate() + m.dias);

        // A ultima parcela absorve a diferenca de arredondamento,
        // garantindo que a soma feche exatamente com o total.
        let valor = Math.round(total * (m.percentual / 100) * 100) / 100;
        if (i === modelo.length - 1) valor = Math.round((total - acumulado) * 100) / 100;
        acumulado += valor;

        if (valor <= 0) return;

        PAGAMENTOS.push({
            idCondicaoPagamento: idCondicao,
            condicaoNome: nomeCondicao(idCondicao),
            idFormaPagamento: m.idFormaPagamento,
            formaNome: nomeForma(m.idFormaPagamento),
            vlParcela: valor,
            dtVencimento: venc.toISOString().slice(0, 10)
        });
    });

    renderPagamentos();
}

// Fornecedor com condicao de pagamento no cadastro: a compra usa a dele e o
// campo fica travado (o servidor tambem garante isso ao gravar). Se a
// condicao dele estiver inativa (fora da lista), o campo fica livre.
function travarCondicaoFornecedor(idCondicao) {
    const sel = document.getElementById('IdCondicaoPagamento');
    if (!sel) return;
    const travar = !!idCondicao && !!sel.querySelector('option[value="' + idCondicao + '"]');
    if (travar) sel.value = idCondicao;
    sel.disabled = travar;
    document.getElementById('btnNovaCondicao').disabled = travar;
    document.getElementById('condicaoFornecedorAviso').classList.toggle('d-none', !travar);
}

// + da condicao de pagamento: usa o mesmo modal do cadastro de fornecedor.
// A flag diz que a condicao salva deve ser selecionada na compra.
let condicaoPelaCompra = false;
function novaCondicaoCompra() {
    condicaoPelaCompra = true;
    limparFormCondicao();
    abrirModalEmpilhado('modalCondicao');
}

// Condicao cadastrada (pelo + daqui ou pelo do cadastro de fornecedor):
// entra na lista da compra; so e selecionada quando veio do + daqui.
document.addEventListener('condicao:salva', e => {
    const pelaCompra = condicaoPelaCompra;
    condicaoPelaCompra = false;
    const sel = document.getElementById('IdCondicaoPagamento');
    if (!sel || !e.detail.ativo) return;

    let opt = sel.querySelector('option[value="' + e.detail.id + '"]');
    if (!opt) {
        opt = document.createElement('option');
        opt.value = e.detail.id;
        sel.appendChild(opt);
    }
    opt.textContent = e.detail.nome;

    const c = CONDICOES.find(x => x.id === e.detail.id);
    if (c) c.nome = e.detail.nome;
    else CONDICOES.push({ id: e.detail.id, nome: e.detail.nome });

    if (pelaCompra && !sel.disabled) sel.value = e.detail.id;
});

// Fechou o modal sem salvar: o proximo cadastro pode vir do fornecedor.
document.getElementById('modalCondicao')?.addEventListener('hidden.bs.modal', () => condicaoPelaCompra = false);

// ---------------- Chave e datas (liberacao por etapas) ----------------

function mensagemChave(html, classe) {
    const el = document.getElementById('chaveMsg');
    if (!el) return;
    el.className = 'small ' + classe;
    el.innerHTML = html;
}

// Etapa 1: confere no servidor se a chave esta completa, se o
// fornecedor existe e se a nota ainda nao foi lancada. Chave aceita, o
// servidor ja grava o rascunho e a tela reabre como edicao dessa compra
// (dali em diante tudo e salvo sozinho, ver "Salvamento automatico").
async function validarChave() {
    if (!texto('NrModelo')) { exibirErro(secaoDoCampo('NrNota'), 'Selecione o modelo da nota.'); return; }
    if (texto('NrSerie') === '') { exibirErro(secaoDoCampo('NrNota'), 'Informe a série da nota.'); document.getElementById('NrSerie').focus(); return; }
    if (!texto('NrNota')) { exibirErro(secaoDoCampo('NrNota'), 'Informe o número da nota.'); document.getElementById('NrNota').focus(); return; }
    if (!texto('IdFornecedor')) { exibirErro(secaoDoCampo('NrNota'), 'Informe o fornecedor.'); document.getElementById('IdFornecedor').focus(); return; }

    const dados = new FormData();
    dados.append('__RequestVerificationToken', tokenAntiforgery());
    CAMPOS_CHAVE.forEach(id => dados.append(id, texto(id)));

    const btn = document.getElementById('btnValidarChave');
    btn.disabled = true;

    let r;
    try {
        const resp = await fetch('?handler=IniciarRascunho', { method: 'POST', body: dados });
        r = await resp.json();
    } catch {
        mensagemChave('<i class="bi bi-x-circle me-1"></i>Não foi possível validar a chave. Tente novamente.', 'text-danger');
        btn.disabled = false;
        return;
    }

    if (r.fornecedor) document.getElementById('nomeFornecedor').value = r.fornecedor;

    if (!r.ok) {
        mensagemChave('<i class="bi bi-x-circle me-1"></i>' + escaparHtml(r.erro), 'text-danger');
        btn.disabled = false;
        return;
    }

    // replace: o Voltar do navegador nao retorna a tela vazia de inclusao.
    mensagemChave('<span class="spinner-border spinner-border-sm me-1"></span>Chave validada. Rascunho criado, abrindo a compra...', 'text-success');
    location.replace(r.url);
}

function hojeIso() {
    const d = new Date();
    return d.getFullYear() + '-' + String(d.getMonth() + 1).padStart(2, '0') + '-' + String(d.getDate()).padStart(2, '0');
}

function marcarErroData(id, msg) {
    document.getElementById(id).classList.toggle('is-invalid', !!msg);
    document.getElementById(id + 'Erro').textContent = msg;
}

// Etapa 2: emissao obrigatoria e ate hoje; chegada opcional, nunca
// antes da emissao (pode ser no mesmo dia) e nunca futura, pois so se
// informa depois que a mercadoria chegou. Retorna o primeiro erro.
function validarDatas() {
    const emissao = texto('DtEmissao');
    const chegada = texto('DtChegada');
    let erroEmissao = '', erroChegada = '';

    // Embaixo do campo a mensagem e so "Data inválida." (pedido do
    // usuario); o motivo fica nos comentarios das regras abaixo.
    const INVALIDA = 'Data inválida.';

    // Emissao: obrigatoria e nunca futura.
    if (!emissao) erroEmissao = 'Informe a data de emissão.';
    else if (emissao > hojeIso()) erroEmissao = INVALIDA;

    // Chegada: nunca futura e nunca antes da emissao. So compara com a
    // emissao quando ela e valida, para nao acusar a chegada a toa.
    if (chegada && chegada > hojeIso())
        erroChegada = INVALIDA;
    else if (chegada && emissao && !erroEmissao && chegada < emissao)
        erroChegada = INVALIDA;

    marcarErroData('DtEmissao', erroEmissao);
    marcarErroData('DtChegada', erroChegada);

    return erroEmissao || erroChegada;
}

// Datas so abrem com a chave validada; o resto da tela, com chave e datas validas.
function aplicarEtapas() {
    if (SOMENTE_LEITURA) return;

    const datasOk = chaveValidada && !validarDatas();
    document.querySelectorAll('fieldset.etapa-datas').forEach(fs => fs.disabled = !chaveValidada);
    document.querySelectorAll('fieldset.etapa-demais').forEach(fs => fs.disabled = !datasOk);
}

// ---------------- Gravacao ----------------

function salvar(status) {
    if (!texto('NrModelo')) { exibirErro(secaoDoCampo('NrNota'), 'Selecione o modelo da nota.'); return; }
    if (!texto('NrNota')) { exibirErro(secaoDoCampo('NrNota'), 'Informe o número da nota.'); document.getElementById('NrNota').focus(); return; }
    if (texto('NrSerie') === '') { exibirErro(secaoDoCampo('NrNota'), 'Informe a série da nota.'); document.getElementById('NrSerie').focus(); return; }
    if (!texto('IdFornecedor')) { exibirErro(secaoDoCampo('NrNota'), 'Informe o fornecedor.'); document.getElementById('IdFornecedor').focus(); return; }
    if (!chaveValidada) { exibirErro(secaoDoCampo('NrNota'), 'Valide a chave da nota antes de salvar.'); return; }

    const erroData = validarDatas();
    if (erroData) {
        exibirErro(secaoDoCampo('NrNota'), erroData);
        // A mensagem e generica: o foco mostra qual data esta errada.
        document.querySelector('#DtEmissao.is-invalid, #DtChegada.is-invalid')?.focus();
        return;
    }

    if (status === 'FINALIZADA') {
        if (!ITENS.length) { exibirErro(secaoDoCampo('itTipo'), 'Adicione ao menos um produto/serviço para finalizar a compra.'); return; }
        if (!PAGAMENTOS.length) { exibirErro(secaoDoCampo('pgValor'), 'Informe ao menos uma parcela de pagamento para finalizar a compra.'); return; }

        const soma = PAGAMENTOS.reduce((a, b) => a + (Number(b.vlParcela) || 0), 0);
        if (Math.abs(soma - totalCompra()) > 0.01) {
            exibirErro(secaoDoCampo('pgValor'), 'A soma das parcelas (R$ ' + moeda(soma) + ') é diferente do total da compra (R$ ' + moeda(totalCompra()) + ').');
            return;
        }

        if (!confirm('Finalizar a compra? Serão gerados os títulos no Contas a Pagar e a entrada no estoque.')) return;
    }

    // Daqui em diante quem grava e o submit: o salvamento automatico para.
    clearTimeout(autoSalvarTimer);
    enviandoFormulario = true;

    prepararEnvio(status);

    // Campos desabilitados nao sao enviados: a chave e reenviada
    // pelos hidden Orig* e reaplicada aqui antes do submit (a condicao
    // de pagamento travada pelo fornecedor tambem).
    CAMPOS_TRAVADOS.forEach(id => {
        const el = document.getElementById(id);
        if (el && el.disabled) el.disabled = false;
    });

    document.getElementById('formCompra').submit();
}

// Campos que ficam desabilitados na edicao mas precisam ir no envio.
const CAMPOS_TRAVADOS = ['NrNota', 'NrSerie', 'NrModelo', 'IdFornecedor', 'IdCondicaoPagamento'];

// Situacao, itens e parcelas vao nos campos ocultos do form.
function prepararEnvio(status) {
    document.getElementById('TpStatus').value = status;

    document.getElementById('ItensJson').value = JSON.stringify(ITENS.map(i => ({
        TpItem: i.tpItem,
        IdProduto: i.idProduto,
        IdServico: i.idServico,
        IdClassificacaoConta: i.idClassificacaoConta,
        SgUnidade: i.sgUnidade,
        QtItem: i.qtItem,
        VlUnitario: i.vlUnitario,
        VlDesconto: i.vlDesconto
    })));

    document.getElementById('PagamentosJson').value = JSON.stringify(PAGAMENTOS.map(p => ({
        IdCondicaoPagamento: p.idCondicaoPagamento,
        IdFormaPagamento: p.idFormaPagamento,
        VlParcela: p.vlParcela,
        DtVencimento: p.dtVencimento
    })));
}

// ---------------- Salvamento automatico do rascunho ----------------
// Compra em rascunho e salva sozinha a cada alteracao (campo do
// cabecalho, item, parcela), com um pequeno atraso para juntar a
// digitacao. Se a energia cair ou a tela fechar, o que ja foi lancado
// continua no banco. Compra finalizada so muda pelo botao Salvar.

const AUTO_SALVAR = DADOS_TELA.AUTO_SALVAR;
let autoSalvarTimer = null;
let autoSalvarEmAndamento = false;
let autoSalvarPendente = false;
let enviandoFormulario = false;
let telaIniciada = false;

function statusAutoSalvar(html, classe) {
    const el = document.getElementById('autoSalvarStatus');
    if (!el) return;
    el.className = 'small ms-auto me-3 ' + classe;
    el.innerHTML = html;
}

function agendarAutoSalvar() {
    if (!AUTO_SALVAR || !telaIniciada || enviandoFormulario) return;
    clearTimeout(autoSalvarTimer);
    statusAutoSalvar('<i class="bi bi-pencil me-1"></i>Alterações ainda não salvas...', 'text-muted');
    autoSalvarTimer = setTimeout(autoSalvar, 1500);
}

// Form completo como no Salvar Rascunho, mas para envio por fetch/beacon.
function dadosRascunho() {
    prepararEnvio('RASCUNHO');
    const dados = new FormData(document.getElementById('formCompra'));
    CAMPOS_TRAVADOS.forEach(id => {
        const el = document.getElementById(id);
        if (el && el.disabled) dados.set(el.name, el.value);
    });
    return dados;
}

async function autoSalvar() {
    autoSalvarTimer = null;
    if (enviandoFormulario) return;
    // Uma gravacao por vez: o que mudar durante ela sai logo em seguida.
    if (autoSalvarEmAndamento) { autoSalvarPendente = true; return; }

    if (validarDatas()) {
        statusAutoSalvar('<i class="bi bi-exclamation-triangle me-1"></i>Rascunho não salvo: corrija as datas.', 'text-danger');
        return;
    }

    autoSalvarEmAndamento = true;
    statusAutoSalvar('<span class="spinner-border spinner-border-sm me-1"></span>Salvando rascunho...', 'text-muted');
    try {
        const resp = await fetch('?handler=AutoSalvar', { method: 'POST', body: dadosRascunho() });
        const r = await resp.json();
        if (r.ok) statusAutoSalvar('<i class="bi bi-cloud-check me-1"></i>Rascunho salvo às ' + r.hora, 'text-success');
        else statusAutoSalvar('<i class="bi bi-exclamation-triangle me-1"></i>Rascunho não salvo: ' + escaparHtml(r.erro), 'text-danger');
    } catch {
        statusAutoSalvar('<i class="bi bi-exclamation-triangle me-1"></i>Não foi possível salvar o rascunho. Verifique a conexão.', 'text-danger');
    } finally {
        autoSalvarEmAndamento = false;
        if (autoSalvarPendente) { autoSalvarPendente = false; autoSalvar(); }
    }
}

// Botao Atualizar (so em rascunho): grava o rascunho na hora e recarrega a
// tela, trazendo o que mudou nos outros cadastros (condicao de pagamento do
// fornecedor, nomes, condicoes, formas, descricao dos produtos/servicos).
// Itens e parcelas ja lancados estao no rascunho, entao nada se perde; se a
// gravacao falhar, a tela nao recarrega e mostra o motivo.
async function atualizarDados() {
    const btn = document.getElementById('btnAtualizar');
    btn.disabled = true;
    clearTimeout(autoSalvarTimer);
    autoSalvarTimer = null;
    while (autoSalvarEmAndamento) await new Promise(r => setTimeout(r, 100));

    let erro = validarDatas();
    if (!erro) {
        try {
            const resp = await fetch('?handler=AutoSalvar', { method: 'POST', body: dadosRascunho() });
            const r = await resp.json();
            if (!r.ok) erro = r.erro;
        } catch {
            erro = 'Não foi possível salvar o rascunho. Verifique a conexão.';
        }
    }
    if (erro) {
        exibirErro(secaoDoCampo('NrNota'), erro);
        btn.disabled = false;
        return;
    }

    enviandoFormulario = true;   // o pagehide nao grava de novo ao recarregar
    location.reload();
}

// Saindo da tela (Voltar, fechar a aba) com alteracao ainda esperando o
// atraso: o navegador envia mesmo com a pagina sendo descarregada.
window.addEventListener('pagehide', () => {
    if (!autoSalvarTimer || enviandoFormulario) return;
    clearTimeout(autoSalvarTimer);
    navigator.sendBeacon('?handler=AutoSalvar', dadosRascunho());
});

// ---------------- Inicializacao ----------------

document.addEventListener('DOMContentLoaded', function () {
    renderItens();
    renderPagamentos();
    aplicarEtapas();
    if (chaveValidada) travarCondicaoFornecedor(CONDICAO_FORNECEDOR);

    // Rascunho: qualquer campo do form (os que tem name) salva sozinho.
    // Os campos de digitacao de item/parcela nao tem name: so contam
    // quando o item ou a parcela entra na grade (renderItens/renderPagamentos).
    ['input', 'change'].forEach(ev => document.getElementById('formCompra').addEventListener(ev, e => {
        if (e.target.name && e.target.type !== 'hidden') agendarAutoSalvar();
    }));
    if (AUTO_SALVAR) statusAutoSalvar('<i class="bi bi-cloud-check me-1"></i>Rascunho salvo automaticamente', 'text-muted');

    ['itQt', 'itVlUnit'].forEach(id =>
        document.getElementById(id)?.addEventListener('input', calcItemTotal));

    document.getElementById('itDesc')?.addEventListener('input', () => { descontoPorPercentual = false; calcItemTotal(); });
    document.getElementById('itDescPerc')?.addEventListener('input', () => { descontoPorPercentual = true; calcItemTotal(); });

    // Custo adicional muda o rateio da grade, nao so os totais.
    ['VlFrete', 'VlSeguro', 'VlOutrasDespesas'].forEach(id =>
        document.getElementById(id)?.addEventListener('input', renderItens));

    ['DtEmissao', 'DtChegada'].forEach(id =>
        document.getElementById(id)?.addEventListener('change', aplicarEtapas));

    // Enter num campo da chave valida a chave (e nao submete o form).
    CAMPOS_CHAVE.forEach(id => document.getElementById(id)?.addEventListener('keydown', e => {
        if (e.key !== 'Enter') return;
        e.preventDefault();
        if (document.getElementById('btnValidarChave')) validarChave();
    }));

    document.getElementById('IdFornecedor')?.addEventListener('change', () =>
        buscarPorId('BuscarFornecedor', 'IdFornecedor', 'nomeFornecedor'));

    document.getElementById('IdTransportador')?.addEventListener('change', () =>
        buscarPorId('BuscarTransportador', 'IdTransportador', 'nomeTransportador'));

    // A partir daqui as alteracoes sao do usuario (o render inicial nao salva).
    telaIniciada = true;
});

