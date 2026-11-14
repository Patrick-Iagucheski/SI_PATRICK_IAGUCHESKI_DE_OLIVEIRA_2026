// Script da tela de Compra (inclusao e edicao) (Pages/Compras/Compra.cshtml).
// Os valores que vem do servidor estao no bloco JSON #dadosTela da pagina.
const DADOS_TELA = JSON.parse(document.getElementById('dadosTela').textContent);

// Tipos de item (constantes da entidade no servidor).
const TIPO_PRODUTO = DADOS_TELA.TIPO_ITEM.PRODUTO;

const FORMAS = DADOS_TELA.FORMAS;
const CONDICOES = DADOS_TELA.CONDICOES;
const SOMENTE_LEITURA = DADOS_TELA.SOMENTE_LEITURA;
const CAMPOS_CHAVE = ['NrModelo', 'NrSerie', 'NrNota', 'IdFornecedor'];

// Serie (ate 3) e numero da nota (ate 9): so digitos.
['NrSerie', 'NrNota'].forEach(id => document.getElementById(id)?.addEventListener('input', e => {
    e.target.value = e.target.value.replace(/\D/g, '');
}));
const CONDICAO_FORNECEDOR = DADOS_TELA.CONDICAO_FORNECEDOR;

// Enquanto a chave nao for validada, nenhum outro campo e editavel.
let chaveValidada = DADOS_TELA.chaveValidada;

// Desconto do item e POR UNIDADE: o total e recalculado ao abrir (rascunho
// gravado antes da regra atual fica certo ao salvar de novo).
let ITENS = DADOS_TELA.ITENS.map(i => ({ ...i, vlTotal: totalItem(i.qtItem, i.vlUnitario, i.vlDesconto) }));
let PAGAMENTOS = DADOS_TELA.PAGAMENTOS;

// Indice em edicao (null = incluindo um novo).
let itemEditandoIdx = null;
let pagamentoEditandoIdx = null;

// ---------------- Utilitarios ----------------

// Quantidade com 2 casas decimais.
function qtd(n) {
    return (Number(n) || 0).toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

// ---- Contas com inteiros ----
// O numero do JavaScript nao guarda decimais exatos (14,3797855 vira
// 14,379785499999999 e arredondaria para baixo). As contas da compra sao
// feitas em inteiros (centavos e milionesimos) e so no fim voltam a decimal,
// para dar o mesmo resultado do servidor (decimal do C#, 5 arredonda para cima).
function centavos(v) { return Math.round((Number(v) || 0) * 100); }
function milionesimos(v) { return Math.round((Number(v) || 0) * 1e6); }

// Valor com mais casas decimais do que o banco grava (mesma regra do servidor,
// Module.PassaDasCasas). Comparado como texto, sem erro de conta do JavaScript.
function passaDasCasas(valor, casas) {
    const decimais = String(valor).split('.')[1] || '';
    return decimais.replace(/0+$/, '').length > casas;
}

// Frete, seguro e outros: 2 casas. Devolve a mensagem de erro ou ''.
function erroCustoAdicional() {
    return ['VlFrete', 'VlSeguro', 'VlOutrasDespesas'].some(id => passaDasCasas(num(id), 2))
        ? 'Frete, seguro e outros gastos aceitam no máximo 2 casas decimais.' : '';
}

// Arredonda em 6 casas decimais (5 para cima).
function arred6(n) {
    return milionesimos(n) / 1e6;
}

// Total do item: quantidade x (preco - desconto por unidade), em 2 casas.
// Mesma conta do servidor (CompraBusiness.CalcularTotalItem).
function totalItem(qt, vlUnit, descUnit) {
    const liquidoUnit = milionesimos(vlUnit) - milionesimos(descUnit);   // milionesimos
    return Math.round(centavos(qt) * liquidoUnit / 1e6) / 100;             // centavos -> R$
}

// Custo unitario: preco - desconto por unidade + rateio / quantidade, em 6 casas.
// Mesma conta do servidor (CompraBusiness.RatearCustoAdicional).
function custoUnitario(qt, vlUnit, descUnit, rateio) {
    const qtCent = centavos(qt);
    if (qtCent <= 0) return 0;
    const custo = milionesimos(vlUnit) - milionesimos(descUnit) + milionesimos(rateio) * 100 / qtCent;
    return Math.round(custo) / 1e6;
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
// Cada pesquisa vem do componente de cadastro da entidade (abrirPesquisaX):
// aqui so o que a compra faz com o registro escolhido, editado ou excluido.

// Fornecedor, transportadora e item sao campos de selecao com lupa (module.js):
// o <select> guarda o id e o nome aparece no campo.
function abrirBuscaFornecedor() {
    abrirPesquisaFornecedor(opcoesDoSelect('IdFornecedor', f => f.razaoSocial));
}

function abrirBuscaTransportador() {
    abrirPesquisaTransportador(opcoesDoSelect('IdTransportador', t => t.razaoSocial));
}

// Autocomplete dos campos que comecam vazios (data-autocomplete, module.js):
// a mesma busca e a mesma selecao da pesquisa de cada um.
// Digitando so numeros (com ou sem mascara), busca pelo documento; senao, pela razao social.
// O documento aparece abaixo do nome, para separar fornecedores com nome parecido.
function autocompleteFornecedor() {
    return { url: urlFornecedores('BuscarFornecedores'),
             param: t => /^[\d.\-\/\s]+$/.test(t) && /\d/.test(t) ? 'documento' : 'razao',
             texto: f => f.razaoSocial, detalhe: documentoPessoa,
             escolher: f => opcoesDoSelect('IdFornecedor', r => r.razaoSocial).selecionar(f) };
}
function autocompleteTransportador() {
    return { url: urlTransportadores('BuscarTransportadores'), param: 'razao', texto: t => t.razaoSocial,
             escolher: t => opcoesDoSelect('IdTransportador', r => r.razaoSocial).selecionar(t) };
}
function autocompleteItem() {
    return { url: urlProdutos('BuscarProdutos'), param: 'termo', texto: p => p.nome,
             escolher: p => aplicarItemSelecionado({ id: p.id, nome: p.nome, unidade: p.unidade, custo: p.custo }) };
}

// A compra so tem produtos: a pesquisa e sempre a de produto.
function abrirBuscaItem() {
    abrirPesquisaProduto({
        atualizado: p => { renomearNoSelect('itCodigo', p.id, p.nome); if (String(p.id) === texto('itCodigo')) definirUnidadeItem(p.unidade); },
        excluido: p => { if (String(p.id) === texto('itCodigo')) limparItemForm(); },
        selecionar: p => aplicarItemSelecionado({ id: p.id, nome: p.nome, unidade: p.unidade, custo: p.custo })
    });
}

function definirUnidadeItem(sigla) {
    document.getElementById('itUnidade').value = sigla || '';
}

// Forma da parcela e condicao: alem do campo, mantem as listas FORMAS e
// CONDICOES (nomes na grade de parcelas).
function abrirBuscaForma() {
    abrirPesquisaForma(opcoesDoSelectComLista('pgForma', FORMAS));
}

function abrirBuscaCondicao() {
    abrirPesquisaCondicao(opcoesDoSelectComLista('IdCondicaoPagamento', CONDICOES));
}

// Preenche os campos do item a partir do produto escolhido: a unidade e a
// do cadastro do produto e o preco sugerido e o custo atual dele.
function aplicarItemSelecionado(reg) {
    selecionarNoSelect('itCodigo', reg.id, reg.nome);
    definirUnidadeItem(reg.unidade);
    // Sugestao de preco: custo atual do produto, nas 4 casas do preco unitario.
    if (reg.custo) document.getElementById('itVlUnit').value = Math.round(reg.custo * 1e4) / 1e4;
    calcItemTotal();
}

// Descricao do item escolhido (texto da opcao do campo de selecao).
function descricaoItem() {
    const sel = document.getElementById('itCodigo');
    return sel.value ? sel.selectedOptions[0].textContent.trim() : '';
}

// ---------------- Itens ----------------

// Qual dos dois campos de desconto o usuario digitou por ultimo: o
// outro e recalculado a partir dele (inclusive quando muda qtd/preco).
let descontoPorPercentual = false;

// Desconto POR UNIDADE: a porcentagem e sobre o preco unitario e o valor (R$)
// e o desconto de cada unidade, com ate 6 casas.
function calcItemTotal() {
    const vlUnit = num('itVlUnit');

    if (descontoPorPercentual) {
        // milionesimos do preco x porcentagem em centesimos / 10000 (em inteiros)
        document.getElementById('itDesc').value = String(Math.round(milionesimos(vlUnit) * centavos(num('itDescPerc')) / 10000) / 1e6);
    } else {
        document.getElementById('itDescPerc').value =
            vlUnit > 0 ? arred2(num('itDesc') / vlUnit * 100).toFixed(2) : '0';
    }

    const total = totalItem(num('itQt'), vlUnit, num('itDesc'));
    document.getElementById('itTotal').value = (total > 0 ? total : 0).toFixed(2);
}

function adicionarItem() {
    const codigo = parseInt(texto('itCodigo')) || 0;
    const descricao = descricaoItem();
    const secao = secaoDoCampo('itCodigo');

    if (!codigo || !descricao) { exibirErro(secao, 'Selecione um produto válido.'); return; }

    const qt = num('itQt');
    if (qt <= 0) { exibirErro(secao, 'A quantidade deve ser maior que zero.'); return; }
    if (qt !== arred2(qt)) { exibirErro(secao, 'A quantidade aceita no máximo 2 casas decimais.'); return; }

    const vlUnit = num('itVlUnit');
    if (vlUnit <= 0) { exibirErro(secao, 'Informe o preço unitário (maior que zero).'); return; }
    if (passaDasCasas(vlUnit, 4)) { exibirErro(secao, 'O preço unitário aceita no máximo 4 casas decimais.'); return; }

    // Desconto por unidade: o valor (R$) e o que vale; a porcentagem so ajuda a calcular.
    const perc = num('itDescPerc');
    const desc = num('itDesc');
    if (perc < 0 || perc > 100) { exibirErro(secao, 'A porcentagem de desconto deve estar entre 0 e 100.'); return; }
    if (desc < 0) { exibirErro(secao, 'O desconto não pode ser negativo.'); return; }
    if (desc !== arred6(desc)) { exibirErro(secao, 'O desconto aceita no máximo 6 casas decimais.'); return; }
    if (desc > vlUnit) {
        exibirErro(secao, 'O desconto por unidade (R$ ' + moeda(desc, 6) + ') é maior que o preço unitário (R$ ' + moeda(vlUnit) + ').');
        return;
    }

    const item = {
        tpItem: TIPO_PRODUTO,
        idProduto: codigo,
        idServico: null,
        codigo: codigo,
        descricao: descricao,
        // A classificacao saiu da tela. Item antigo que ja tinha uma
        // a mantem ao ser editado, para nao perder o que foi gravado.
        idClassificacaoConta: itemEditandoIdx !== null ? (ITENS[itemEditandoIdx].idClassificacaoConta ?? null) : null,
        sgUnidade: document.getElementById('itUnidade').value || null,
        qtItem: qt,
        vlUnitario: vlUnit,
        vlDesconto: desc,
        vlTotal: totalItem(qt, vlUnit, desc)
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

    selecionarNoSelect('itCodigo', it.codigo, it.descricao);
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
        tb.innerHTML = '<tr><td colspan="10" class="text-center text-muted py-3">Nenhum produto adicionado</td></tr>';
    }

    const rateios = calcRateio();

    ITENS.forEach((it, idx) => {
        const acoes = SOMENTE_LEITURA ? '<td></td>' :
            '<td class="text-center" style="white-space:nowrap;">' +
            '<button type="button" class="btn btn-icon btn-outline-primary" title="Editar" onclick="editarItem(' + idx + ')"><i class="bi bi-pencil"></i></button> ' +
            '<button type="button" class="btn btn-icon btn-outline-danger" title="Excluir" onclick="removerItem(' + idx + ')"><i class="bi bi-trash"></i></button></td>';

        const percDesc = it.vlUnitario > 0 && it.vlDesconto > 0
            ? '<div class="text-muted small">' + moeda(it.vlDesconto / it.vlUnitario * 100) + '% por unidade</div>' : '';
        // Preco - desconto por unidade + rateio por unidade, com 6 casas como o
        // servidor (5 para cima, abaixo de 5 para baixo). Ex.: 4,97 - 0,06 = 4,910000.
        const custoUnit = custoUnitario(it.qtItem, it.vlUnitario, it.vlDesconto, rateios[idx]);

        const tr = document.createElement('tr');
        tr.innerHTML =
            '<td>' + (idx + 1) + '</td>' +
            '<td><strong>' + escaparHtml(it.descricao) + '</strong>' +
                '<div class="text-muted small">Cód. ' + it.codigo + '</div></td>' +
            '<td class="text-center">' + escaparHtml(it.sgUnidade || '-') + '</td>' +
            '<td class="text-end">' + qtd(it.qtItem) + '</td>' +
            '<td class="text-end">R$ ' + moeda(it.vlUnitario) + '</td>' +
            '<td class="text-end">R$ ' + moeda(it.vlDesconto, 6) + percDesc + '</td>' +
            '<td class="text-end"><strong>R$ ' + moeda(it.vlTotal) + '</strong></td>' +
            '<td class="text-end">R$ ' + moeda(rateios[idx], 6) + '</td>' +
            '<td class="text-end">R$ ' + moeda(custoUnit, 6) + '</td>' +
            acoes;
        tb.appendChild(tr);
    });

    // Totalizador da grade.
    const soma = campo => ITENS.reduce((a, b) => a + (Number(b[campo]) || 0), 0);
    document.getElementById('itensFoot').style.display = ITENS.length ? '' : 'none';
    document.getElementById('totQtd').textContent = qtd(soma('qtItem'));
    // Desconto total = soma de quantidade x desconto por unidade.
    document.getElementById('totDesconto').textContent =
        'R$ ' + moeda(ITENS.reduce((a, b) => a + Math.round(centavos(b.qtItem) * milionesimos(b.vlDesconto) / 1e6), 0) / 100);
    document.getElementById('totItens').textContent = 'R$ ' + moeda(soma('vlTotal'));
    document.getElementById('totRateio').textContent = 'R$ ' + moeda(rateios.reduce((a, b) => a + b, 0), 6);

    document.getElementById('contadorItens').textContent =
        ITENS.length + (ITENS.length === 1 ? ' item' : ' itens');

    calcTotais();
    agendarAutoSalvar();
}

// Rateio do custo adicional (frete + seguro + outros) entre os PRODUTOS,
// proporcional ao total de cada um. O ultimo produto absorve a diferenca
// de arredondamento. Mesma conta de RatearCustoAdicional no servidor.
function calcRateio() {
    // Em inteiros: custo em milionesimos e totais em centavos (ver centavos()).
    const custo = milionesimos(custoAdicional());
    const produtos = ITENS.map((it, i) => it.tpItem === TIPO_PRODUTO ? i : -1).filter(i => i >= 0);
    const base = produtos.reduce((a, i) => a + centavos(ITENS[i].vlTotal), 0);
    const rateios = ITENS.map(() => 0);
    let acumulado = 0;

    produtos.forEach((i, n) => {
        const valor = n === produtos.length - 1
            ? custo - acumulado
            : Math.round(base > 0 ? custo * centavos(ITENS[i].vlTotal) / base : custo / produtos.length);
        acumulado += valor;
        rateios[i] = valor / 1e6;
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

// Total da compra (valor da nota, regra da NF-e: produtos - desconto + frete +
// seguro + outros) = total dos produtos ja com o desconto + custo adicional.
// Ex.: 279,72 + 71,51 + 13,11 = 364,34. Somado em centavos (sem erro de
// casa decimal). Mesma conta do servidor (CompraBusiness.Salvar).
function totalCompra() {
    const produtos = ITENS.reduce((a, b) => a + centavos(b.vlTotal), 0);
    return (produtos + centavos(custoAdicional())) / 100;
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
    } else if (Math.round(diferenca * 100) === 0) {
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
    if (passaDasCasas(valor, 2)) { exibirErro(secaoDoCampo('pgValor'), 'O valor da parcela aceita no máximo 2 casas decimais.'); return; }

    const venc = texto('pgVencimento');
    if (!venc) { exibirErro(secaoDoCampo('pgValor'), 'Informe a data de vencimento.'); return; }
    // Mesma regra do servidor: a parcela nao vence antes da emissao da nota.
    const emissao = texto('DtEmissao');
    if (emissao && venc < emissao) { exibirErro(secaoDoCampo('pgValor'), 'O vencimento não pode ser anterior à emissão da nota (' + dataBr(emissao) + ').'); return; }

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
    // Travado, o campo fica desabilitado e a lupa some (module.js).
    sel.disabled = travar;
    document.getElementById('condicaoFornecedorAviso').classList.toggle('d-none', !travar);
}

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
    if (!texto('IdFornecedor')) { exibirErro(secaoDoCampo('NrNota'), 'Informe o fornecedor.'); document.getElementById('IdFornecedorNome').focus(); return; }

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

    if (r.fornecedor) renomearNoSelect('IdFornecedor', texto('IdFornecedor'), r.fornecedor);

    if (!r.ok) {
        mensagemChave('<i class="bi bi-x-circle me-1"></i>' + escaparHtml(r.erro), 'text-danger');
        btn.disabled = false;
        return;
    }

    // replace: o Voltar do navegador nao retorna a tela vazia de inclusao.
    mensagemChave('<span class="spinner-border spinner-border-sm me-1"></span>Chave validada. Rascunho criado, abrindo a compra...', 'text-success');
    location.replace(r.url);
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
    if (!texto('IdFornecedor')) { exibirErro(secaoDoCampo('NrNota'), 'Informe o fornecedor.'); document.getElementById('IdFornecedorNome').focus(); return; }
    if (!chaveValidada) { exibirErro(secaoDoCampo('NrNota'), 'Valide a chave da nota antes de salvar.'); return; }

    const erroCusto = erroCustoAdicional();
    if (erroCusto) { exibirErro(secaoDoCampo('VlFrete'), erroCusto); return; }

    const erroData = validarDatas();
    if (erroData) {
        exibirErro(secaoDoCampo('NrNota'), erroData);
        // A mensagem e generica: o foco mostra qual data esta errada.
        document.querySelector('#DtEmissao.is-invalid, #DtChegada.is-invalid')?.focus();
        return;
    }

    if (status === 'FINALIZADA') {
        if (!ITENS.length) { exibirErro(secaoDoCampo('itCodigo'), 'Adicione ao menos um produto para finalizar a compra.'); return; }
        if (!PAGAMENTOS.length) { exibirErro(secaoDoCampo('pgValor'), 'Informe ao menos uma parcela de pagamento para finalizar a compra.'); return; }

        const soma = PAGAMENTOS.reduce((a, b) => a + (Number(b.vlParcela) || 0), 0);
        if (Math.round(soma * 100) !== Math.round(totalCompra() * 100)) {
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

    // A partir daqui as alteracoes sao do usuario (o render inicial nao salva).
    telaIniciada = true;
});

