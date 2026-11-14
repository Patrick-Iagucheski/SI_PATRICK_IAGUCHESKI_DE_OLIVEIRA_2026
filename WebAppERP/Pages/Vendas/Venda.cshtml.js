// Script da tela de Venda (inclusao e edicao) (Pages/Vendas/Venda.cshtml).
// Os valores que vem do servidor estao no bloco JSON #dadosTela da pagina.
const DADOS_TELA = JSON.parse(document.getElementById('dadosTela').textContent);

// Tipos de item (constantes da entidade no servidor).
const TIPO_PRODUTO = DADOS_TELA.TIPO_ITEM.PRODUTO;
const TIPO_SERVICO = DADOS_TELA.TIPO_ITEM.SERVICO;

const PRODUTOS = DADOS_TELA.PRODUTOS;
const SERVICOS = DADOS_TELA.SERVICOS;
const SOMENTE_LEITURA = DADOS_TELA.SOMENTE_LEITURA;
let ITENS = DADOS_TELA.ITENS;

function num(id) { const v = document.getElementById(id).value; return parseFloat(String(v).replace(',', '.')) || 0; }

// Quantidade x preco arredondado em 2 casas antes do desconto, como o servidor
// (Module.CalcularTotalItem): a tela mostra o mesmo total que sera gravado.
function brutoItem(qt, unit) { return Math.round(qt * unit * 100 + Number.EPSILON) / 100; }

function onTipoChange() {
    const t = document.getElementById('itTipo').value;
    document.getElementById('grpProduto').style.display = t === TIPO_PRODUTO ? '' : 'none';
    document.getElementById('grpServico').style.display = t === TIPO_SERVICO ? '' : 'none';
    document.getElementById('itVlUnit').value = '';
    document.getElementById('itTotal').value = '';
    mostrarSaldo(null);
    mostrarImagemProduto(null);
}

// Saldo do produto escolhido: verde com estoque, vermelho (0) sem estoque.
// Fica abaixo do campo sem ocupar altura (.dica-abaixo, site.css).
function mostrarSaldo(p) {
    const el = document.getElementById('itSaldo');
    const saldo = p ? Math.max(Number(p.saldo) || 0, 0) : 0;
    el.textContent = p ? 'Saldo em estoque: ' + moeda(saldo) : '';
    el.classList.toggle('text-success', !!p && saldo > 0);
    el.classList.toggle('text-danger', !!p && saldo <= 0);
}

// Imagem do produto escolhido. So aparece depois de carregar: sem imagem ou
// com o arquivo ausente, o bloco continua escondido.
function mostrarImagemProduto(p) {
    const wrap = document.getElementById('itImagemWrap');
    const img = document.getElementById('itImagem');
    wrap.classList.add('d-none');
    // removeAttribute em vez de src = '': src vazio dispara o erro de carregamento.
    if (!p?.imagem) { img.removeAttribute('src'); return; }
    const endereco = enderecoNoSite(p.imagem);
    // Mesma imagem ja carregada (o navegador nao carrega de novo): mostra direto.
    if (img.getAttribute('src') === endereco && img.complete && img.naturalWidth > 0) wrap.classList.remove('d-none');
    else img.src = endereco;
}

function onProdutoChange() {
    const id = parseInt(document.getElementById('itProduto').value) || 0;
    const p = PRODUTOS.find(x => x.id === id);
    mostrarSaldo(p);
    mostrarImagemProduto(p);
    if (p) document.getElementById('itVlUnit').value = p.valor;
    calcItemTotal();
}

function onServicoChange() {
    const id = parseInt(document.getElementById('itServico').value) || 0;
    const s = SERVICOS.find(x => x.id === id);
    if (s) document.getElementById('itVlUnit').value = s.valor;
    calcItemTotal();
}

// ---------------- Pesquisa de produto / servico (a lupa do item) ----------------
// A pesquisa vem do componente de cadastro; aqui so a lista local (PRODUTOS /
// SERVICOS, usada para preco e saldo) junto com o <select> do item.
function abrirBuscaProdutoVenda() {
    abrirPesquisaProduto(opcoesDoSelectComLista('itProduto', PRODUTOS,
        p => ({ id: p.id, nome: p.nome, saldo: p.estoque ?? 0, valor: p.venda ?? 0, imagem: p.imagem })));
}

function abrirBuscaServicoVenda() {
    abrirPesquisaServico(opcoesDoSelectComLista('itServico', SERVICOS,
        s => ({ id: s.id, nome: s.nome, valor: s.valor ?? 0 })));
}

function calcItemTotal() {
    const total = brutoItem(num('itQt'), num('itVlUnit')) - num('itDesc');
    document.getElementById('itTotal').value = (total > 0 ? total : 0).toFixed(2);
}

// Item aberto no formulario pelo lapis (indice em ITENS); -1: item novo. O item
// continua na grade (e gravado) ate o Atualizar: sair da tela no meio da
// edicao nao perde o item.
let itemEmEdicao = -1;

function botaoAdicionar(editando) {
    document.getElementById('btnAdicionarItem').innerHTML = editando
        ? '<i class="bi bi-check-lg me-1"></i>Atualizar'
        : '<i class="bi bi-plus-lg me-1"></i>Adicionar';
}

function adicionarItem() {
    const t = document.getElementById('itTipo').value;
    let item;
    if (t === TIPO_PRODUTO) {
        const id = parseInt(document.getElementById('itProduto').value) || 0;
        const p = PRODUTOS.find(x => x.id === id);
        if (!p) { exibirErro(secaoDoCampo('itTipo'), 'Selecione um produto.'); return; }
        item = { tpItem: TIPO_PRODUTO, idProduto: id, idServico: null, descricao: p.nome };
    } else {
        const id = parseInt(document.getElementById('itServico').value) || 0;
        const s = SERVICOS.find(x => x.id === id);
        if (!s) { exibirErro(secaoDoCampo('itTipo'), 'Selecione um serviço.'); return; }
        item = { tpItem: TIPO_SERVICO, idServico: id, idProduto: null, descricao: s.nome };
    }
    const qt = num('itQt');
    if (qt <= 0) { exibirErro(secaoDoCampo('itTipo'), 'Informe a quantidade.'); return; }
    const vlUnit = num('itVlUnit');
    if (vlUnit <= 0) { exibirErro(secaoDoCampo('itTipo'), 'Informe o valor unitário (maior que zero).'); return; }
    const desc = num('itDesc');
    if (desc < 0) { exibirErro(secaoDoCampo('itTipo'), 'O desconto não pode ser negativo.'); return; }
    const total = brutoItem(qt, vlUnit) - desc;
    if (total <= 0) { exibirErro(secaoDoCampo('itTipo'), 'O desconto não pode ser igual ou maior que o valor do item.'); return; }
    item.qtItem = qt; item.vlUnitario = vlUnit; item.vlDesconto = desc; item.vlTotal = total;

    // Validacao de estoque (produto): considera o que ja esta no carrinho.
    if (item.tpItem === TIPO_PRODUTO) {
        const p = PRODUTOS.find(x => x.id === item.idProduto);
        const noCarrinho = ITENS.filter((i, idx) => idx !== itemEmEdicao && i.tpItem === TIPO_PRODUTO && i.idProduto === item.idProduto)
                                .reduce((a, b) => a + b.qtItem, 0);
        if (noCarrinho + qt > p.saldo) {
            if (!confirm('Quantidade acima do saldo em estoque (' + moeda(p.saldo) + '). A venda não poderá ser FINALIZADA assim. Adicionar mesmo assim?')) return;
        }
    }

    if (itemEmEdicao >= 0) ITENS[itemEmEdicao] = item;
    else ITENS.push(item);
    limparItemForm();
    renderItens();
}

function limparItemForm() {
    itemEmEdicao = -1;
    botaoAdicionar(false);
    document.getElementById('itProduto').value = '';
    document.getElementById('itServico').value = '';
    document.getElementById('itQt').value = '1';
    document.getElementById('itVlUnit').value = '';
    document.getElementById('itDesc').value = '0';
    document.getElementById('itTotal').value = '';
    mostrarSaldo(null);
    mostrarImagemProduto(null);
}

function removerItem(i) {
    ITENS.splice(i, 1);
    if (i === itemEmEdicao) limparItemForm();
    else if (i < itemEmEdicao) itemEmEdicao--;
    renderItens();
}

function editarItem(i) {
    const it = ITENS[i];
    document.getElementById('itTipo').value = it.tpItem; onTipoChange();
    if (it.tpItem === TIPO_PRODUTO) { document.getElementById('itProduto').value = it.idProduto; onProdutoChange(); }
    else { document.getElementById('itServico').value = it.idServico; }
    document.getElementById('itQt').value = it.qtItem;
    document.getElementById('itVlUnit').value = it.vlUnitario;
    document.getElementById('itDesc').value = it.vlDesconto;
    calcItemTotal();
    itemEmEdicao = i;
    botaoAdicionar(true);
    renderItens(false);   // so destaca a linha: a venda nao mudou
}

function renderItens(salvar = true) {
    const tb = document.getElementById('itensBody');
    tb.innerHTML = '';
    if (ITENS.length === 0) {
        tb.innerHTML = '<tr><td colspan="8" class="text-center text-muted py-3">Nenhum item adicionado</td></tr>';
    }
    ITENS.forEach((it, idx) => {
        const codigo = it.tpItem === TIPO_PRODUTO ? it.idProduto : it.idServico;
        const badge = it.tpItem === TIPO_PRODUTO ? 'badge-agendado' : 'badge-confirmado';
        const acoes = SOMENTE_LEITURA ? '<td></td>' :
            '<td class="text-center" style="white-space:nowrap;">' +
            '<button type="button" class="btn btn-icon btn-outline-primary" title="Editar" onclick="editarItem(' + idx + ')"><i class="bi bi-pencil"></i></button> ' +
            '<button type="button" class="btn btn-icon btn-outline-danger" title="Remover" onclick="removerItem(' + idx + ')"><i class="bi bi-trash"></i></button></td>';
        const tr = document.createElement('tr');
        if (idx === itemEmEdicao) tr.className = 'table-warning';
        tr.innerHTML =
            '<td><span class="badge-status ' + badge + '">' + it.tpItem + '</span></td>' +
            '<td>' + codigo + '</td>' +
            '<td><strong>' + escaparHtml(it.descricao) + '</strong></td>' +
            '<td class="text-end">' + moeda(it.qtItem) + '</td>' +
            '<td class="text-end">R$ ' + moeda(it.vlUnitario) + '</td>' +
            '<td class="text-end">R$ ' + moeda(it.vlDesconto) + '</td>' +
            '<td class="text-end"><strong>R$ ' + moeda(it.vlTotal) + '</strong></td>' +
            acoes;
        tb.appendChild(tr);
    });
    calcTotais();
    if (salvar) agendarAutoSalvar();
}

// Desconto geral negativo nao e desconto (aumentaria o total): conta como zero
// no total exibido. O aviso aparece no salvamento automatico (o servidor
// recusa) e no Finalizar/Salvar (salvar).
function calcTotais() {
    const subtotal = ITENS.reduce((a, b) => a + b.vlTotal, 0);
    const desc = Math.max(num('VlDesconto'), 0);
    let total = subtotal - desc;
    if (total < 0) total = 0;
    document.getElementById('lblSubtotal').textContent = 'R$ ' + moeda(subtotal);
    document.getElementById('lblTotal').textContent = 'R$ ' + moeda(total);
    // Desconto digitado antes dos itens: a venda aberta grava, so nao finaliza.
    document.getElementById('avisoDesconto')?.classList.toggle('d-none', !(desc > 0 && desc >= subtotal));
}

function prepararEnvio(status) {
    document.getElementById('TpStatus').value = status;
    document.getElementById('ItensJson').value = JSON.stringify(ITENS.map(i => ({
        TpItem: i.tpItem, IdServico: i.idServico, IdProduto: i.idProduto,
        QtItem: i.qtItem, VlUnitario: i.vlUnitario, VlDesconto: i.vlDesconto
    })));
}

async function salvar(status) {
    const func = document.getElementById('IdFuncionario');
    if (!func.value) { exibirErro(secaoDoCampo('IdFuncionario'), 'Selecione o funcionário responsável.'); func.focus(); return; }
    if (num('VlDesconto') < 0) { exibirErro(secaoDoCampo('VlDesconto'), 'O desconto da venda não pode ser negativo.'); return; }
    if (status === 'FINALIZADA' && ITENS.length === 0) { exibirErro(secaoDoCampo('itTipo'), 'Adicione ao menos um item para finalizar.'); return; }
    if (status === 'FINALIZADA') {
        const subtotal = ITENS.reduce((a, b) => a + b.vlTotal, 0);
        if (num('VlDesconto') > 0 && num('VlDesconto') >= subtotal) {
            exibirErro(secaoDoCampo('VlDesconto'), 'O desconto da venda (R$ ' + moeda(num('VlDesconto')) + ') não pode ser igual ou maior que o subtotal (R$ ' + moeda(subtotal) + ').');
            return;
        }
    }
    if (status === 'FINALIZADA' && !document.getElementById('IdFormaPagamento').value && !document.getElementById('IdCondicaoPagamento').value) { exibirErro(secaoDoCampo('IdFormaPagamento'), 'Selecione a forma ou a condição de pagamento para finalizar a venda.'); return; }

    // Espera a gravacao automatica em andamento: ela pode estar criando esta venda.
    enviandoFormulario = true;
    clearTimeout(autoSalvarTimer);
    autoSalvarTimer = null;
    while (autoSalvarEmAndamento) await new Promise(r => setTimeout(r, 100));

    prepararEnvio(status);
    document.getElementById('formVenda').submit();
}

// Com condicao de pagamento, a forma vem das parcelas dela: o campo fica travado.
function travarFormaPelaCondicao() {
    const condicao = document.getElementById('IdCondicaoPagamento');
    if (condicao.disabled) return;
    document.getElementById('IdFormaPagamento').dataset.somenteLeitura = condicao.value ? '1' : '';
}

function aoTrocarCondicao() {
    if (document.getElementById('IdCondicaoPagamento').value) document.getElementById('IdFormaPagamento').value = '';
    travarFormaPelaCondicao();
}

// Ao escolher o cliente, sugere a condicao de pagamento dele (o usuario pode trocar).
// Cliente sem condicao so limpa o campo se o valor atual veio do cliente anterior,
// para nao apagar uma condicao escolhida a mao.
let condicaoDoCliente = '';

async function aoTrocarCliente() {
    const cliente = document.getElementById('IdCliente');
    const idCliente = cliente.value;
    let sugerida = null;
    if (idCliente) {
        try {
            const resp = await fetch('?handler=CondicaoDoCliente&idCliente=' + encodeURIComponent(idCliente));
            sugerida = await resp.json();
        } catch {
            return; // sem resposta, mantem o que esta na tela
        }
        if (cliente.value !== idCliente) return; // trocou de cliente enquanto buscava
    }

    const condicao = document.getElementById('IdCondicaoPagamento');
    if (sugerida?.id) {
        condicaoDoCliente = String(sugerida.id);
        if (condicao.value !== condicaoDoCliente) selecionarNoSelect('IdCondicaoPagamento', sugerida.id, sugerida.nome);
    } else {
        if (condicaoDoCliente && condicao.value === condicaoDoCliente) {
            condicao.value = '';
            condicao.dispatchEvent(new Event('change', { bubbles: true }));
        }
        condicaoDoCliente = '';
    }
}

// ---------------- Agendamento ----------------
// Ao escolher o agendamento (lista ou pesquisa), a venda puxa o que ele ja tem:
// o servico entra nos itens (1 unidade, preco atual, sem repetir se ja esta na
// grade) e cliente e funcionario vazios recebem os do agendamento. O que ja foi
// escolhido na venda nao e trocado. Cadastro inativo nao entra (o servidor
// recusa vinculo novo inativo): fica o aviso.
async function aoTrocarAgendamento() {
    const sel = document.getElementById('IdAgendamento');
    const id = sel.value;
    if (!id) return;

    let ag;
    try {
        const url = document.getElementById('modalAgendamento').dataset.urlAgendamentos
                  + '?handler=BuscarAgendamentos&id=' + encodeURIComponent(id);
        ag = (await (await fetch(url)).json())[0];
    } catch {
        return; // sem resposta: o usuario adiciona o servico pela grade
    }
    if (!ag || sel.value !== id) return; // trocou de agendamento enquanto buscava

    // Ja cobrado em outra venda ou de outro cliente: o servidor recusa; nada e puxado.
    const avisar = (...textos) => mostrarAviso(secaoDoCampo('IdAgendamento'),
        '<div class="alert alert-warning py-2 mb-3"><i class="bi bi-info-circle me-1"></i>' + textos.map(escaparHtml).join('<br>') + '</div>');
    if (ag.idVenda && ag.idVenda !== Number(campoIdVenda().value)) {
        avisar('O agendamento #' + ag.id + ' já está na venda Nº ' + ag.idVenda + '. Escolha outro agendamento.');
        return;
    }
    const clienteVenda = document.getElementById('IdCliente').value;
    if (clienteVenda && ag.idCliente && clienteVenda !== String(ag.idCliente)) {
        avisar('O agendamento #' + ag.id + ' é do cliente ' + ag.cliente + ', diferente do cliente da venda.');
        return;
    }

    const avisos = [];
    const temOpcao = (idSelect, valor) => [...document.getElementById(idSelect).options].some(o => o.value === String(valor));
    const preencher = (idSelect, valor, nome, rotulo) => {
        const campo = document.getElementById(idSelect);
        if (campo.value || !valor) return;
        if (!temOpcao(idSelect, valor)) { avisos.push(rotulo + ' do agendamento (' + nome + ') está inativo e não foi preenchido.'); return; }
        campo.value = String(valor);
        campo.dispatchEvent(new Event('change', { bubbles: true }));
    };
    preencher('IdCliente', ag.idCliente, ag.cliente, 'O cliente');
    preencher('IdFuncionario', ag.idFuncionario, ag.profissional, 'O profissional');

    const jaNaGrade = ITENS.some(i => i.tpItem === TIPO_SERVICO && i.idServico === ag.idServico);
    const servico = SERVICOS.find(s => s.id === ag.idServico);
    if (!jaNaGrade && servico) {
        const vlTotal = brutoItem(1, servico.valor);
        ITENS.push({ tpItem: TIPO_SERVICO, idServico: servico.id, idProduto: null, descricao: servico.nome,
                     qtItem: 1, vlUnitario: servico.valor, vlDesconto: 0, vlTotal });
        renderItens();
        agendarAutoSalvar();
    } else if (!jaNaGrade && ag.idServico) {
        avisos.push('O serviço do agendamento (' + ag.servico + ') está inativo e não foi adicionado.');
    }

    if (avisos.length) avisar(...avisos);
}

// ---------------- Salvamento automatico da venda aberta ----------------
// Venda aberta e salva sozinha a cada alteracao (campo do cabecalho ou item),
// com um pequeno atraso para juntar a digitacao. A primeira gravacao acontece
// quando ha funcionario, o unico campo obrigatorio da venda aberta.
// Venda finalizada ou cancelada nao muda (so visualizacao).

const AUTO_SALVAR = DADOS_TELA.AUTO_SALVAR;
let autoSalvarTimer = null;
let autoSalvarEmAndamento = false;
let autoSalvarPendente = false;
let enviandoFormulario = false;
let telaIniciada = false;

function statusAutoSalvar(html, classe) {
    const el = document.getElementById('autoSalvarStatus');
    el.className = 'small align-self-center ' + classe;
    el.innerHTML = html;
}

function temFuncionario() { return !!document.getElementById('IdFuncionario').value; }
function campoIdVenda() { return document.querySelector('[name="IdVenda"]'); }

function agendarAutoSalvar() {
    if (!AUTO_SALVAR || !telaIniciada || enviandoFormulario) return;
    clearTimeout(autoSalvarTimer);
    statusAutoSalvar('<i class="bi bi-pencil me-1"></i>Alterações ainda não salvas...', 'text-muted');
    autoSalvarTimer = setTimeout(autoSalvar, 1500);
}

function dadosAutoSalvar() {
    prepararEnvio(DADOS_TELA.ABERTA);
    return new FormData(document.getElementById('formVenda'));
}

// Primeira gravacao: dali em diante a tela altera esta mesma venda.
function assumirVendaGravada(id) {
    const campo = campoIdVenda();
    if (Number(campo.value) === id) return;
    campo.value = id;
    document.getElementById('vendaNumero').value = '#' + id;
    document.getElementById('tituloVenda').textContent = 'Venda #' + id;
    history.replaceState(null, '', '?id=' + id);
    avisarGravacao();
}

async function autoSalvar() {
    autoSalvarTimer = null;
    if (enviandoFormulario) return;
    if (!temFuncionario()) {
        statusAutoSalvar('<i class="bi bi-info-circle me-1"></i>Selecione o funcionário para salvar a venda.', 'text-muted');
        return;
    }
    // Uma gravacao por vez: o que mudar durante ela sai logo em seguida.
    if (autoSalvarEmAndamento) { autoSalvarPendente = true; return; }

    autoSalvarEmAndamento = true;
    statusAutoSalvar('<span class="spinner-border spinner-border-sm me-1"></span>Salvando a venda...', 'text-muted');
    try {
        const resp = await fetch('?handler=AutoSalvar', { method: 'POST', body: dadosAutoSalvar() });
        const r = await resp.json();
        if (r.ok) {
            assumirVendaGravada(r.id);
            statusAutoSalvar('<i class="bi bi-cloud-check me-1"></i>Venda salva às ' + r.hora, 'text-success');
        } else {
            statusAutoSalvar('<i class="bi bi-exclamation-triangle me-1"></i>Venda não salva: ' + escaparHtml(r.erro), 'text-danger');
        }
    } catch {
        statusAutoSalvar('<i class="bi bi-exclamation-triangle me-1"></i>Não foi possível salvar a venda. Verifique a conexão.', 'text-danger');
    } finally {
        autoSalvarEmAndamento = false;
        if (autoSalvarPendente) { autoSalvarPendente = false; autoSalvar(); }
    }
}

// Saindo da tela com alteracao ainda esperando o atraso: o navegador envia
// mesmo com a pagina sendo descarregada. Se a primeira gravacao ainda esta em
// andamento, nao envia de novo (criaria outra venda).
window.addEventListener('pagehide', () => {
    if (!autoSalvarTimer || enviandoFormulario || !temFuncionario()) return;
    if (autoSalvarEmAndamento && Number(campoIdVenda().value) === 0) return;
    clearTimeout(autoSalvarTimer);
    navigator.sendBeacon('?handler=AutoSalvar', dadosAutoSalvar());
});

// ---------------- Botao Atualizar ----------------

// Troca as opcoes do campo pelas atuais, mantendo a escolha. A opcao escolhida
// que saiu da lista (ex.: cliente inativado) continua, para nao perder o valor.
function recarregarOpcoes(id, lista) {
    const sel = document.getElementById(id);
    if (!sel) return;
    const atual = sel.value;
    const escolhida = sel.selectedOptions[0];
    [...sel.options].forEach(o => { if (o.value !== '' && o !== escolhida) o.remove(); });
    lista.forEach(item => {
        const manter = String(item.id) === atual;
        const opt = manter ? escolhida : document.createElement('option');
        opt.value = item.id;
        opt.textContent = item.nome;
        definirDetalheOpcao(opt, item.detalhe ?? (manter ? undefined : null));
        sel.appendChild(opt);
    });
    sel.value = atual;
}

// Nome sempre; preco so em venda aberta (venda finalizada mantem o preco vendido).
function atualizarItens(precos) {
    ITENS.forEach(it => {
        const atual = it.tpItem === TIPO_PRODUTO ? PRODUTOS.find(p => p.id === it.idProduto)
                                                 : SERVICOS.find(s => s.id === it.idServico);
        if (!atual) return;
        it.descricao = atual.nome;
        if (precos) {
            it.vlUnitario = atual.valor;
            it.vlTotal = Math.max(brutoItem(it.qtItem, it.vlUnitario) - it.vlDesconto, 0);
        }
    });
    renderItens();
}

// Busca os cadastros de novo e atualiza a tela sem gravar a venda: o que ja
// foi digitado continua aqui.
async function atualizarDados() {
    const btn = document.getElementById('btnAtualizar');
    const secao = secaoDoCampo('IdCliente');
    btn.disabled = true;
    try {
        const resp = await fetch('?handler=AtualizarDados&id=' + encodeURIComponent(campoIdVenda().value));
        if (!resp.ok) throw new Error();
        const d = await resp.json();

        PRODUTOS.splice(0, PRODUTOS.length, ...d.produtos);
        SERVICOS.splice(0, SERVICOS.length, ...d.servicos);
        recarregarOpcoes('IdCliente', d.clientes);
        recarregarOpcoes('IdFuncionario', d.funcionarios);
        recarregarOpcoes('IdAgendamento', d.agendamentos);
        recarregarOpcoes('IdFormaPagamento', d.formas);
        recarregarOpcoes('IdCondicaoPagamento', d.condicoes);
        recarregarOpcoes('itProduto', d.produtos);
        recarregarOpcoes('itServico', d.servicos);
        atualizarItens(d.emAberto);

        // Item que esta sendo digitado: saldo e preco atuais.
        if (document.getElementById('itProduto').value) onProdutoChange();
        if (document.getElementById('itServico').value) onServicoChange();
        if (d.emAberto) await aoTrocarCliente();

        exibirSucesso(secao, d.emAberto ? 'Cadastros, preços e condição do cliente atualizados.'
                                          : 'Cadastros atualizados. Os preços da venda finalizada não mudam.');
    } catch {
        exibirErro(secao, 'Não foi possível atualizar os dados. Verifique a conexão.');
    } finally {
        btn.disabled = false;
    }
}

document.addEventListener('DOMContentLoaded', function () {
    // Imagem do item: aparece so quando carrega de fato (venda finalizada nao tem o bloco).
    const imagemItem = document.getElementById('itImagem');
    imagemItem?.addEventListener('load', () => {
        if (imagemItem.getAttribute('src')) document.getElementById('itImagemWrap').classList.remove('d-none');
    });
    imagemItem?.addEventListener('error', () => document.getElementById('itImagemWrap').classList.add('d-none'));

    document.getElementById('IdCliente').addEventListener('change', aoTrocarCliente);
    document.getElementById('IdAgendamento').addEventListener('change', aoTrocarAgendamento);
    document.getElementById('IdCondicaoPagamento').addEventListener('change', aoTrocarCondicao);
    travarFormaPelaCondicao();
    onTipoChange();
    renderItens();
    document.getElementById('VlDesconto')?.addEventListener('input', calcTotais);
    ['itQt', 'itVlUnit', 'itDesc'].forEach(id => document.getElementById(id)?.addEventListener('input', calcItemTotal));

    // Venda aberta: qualquer campo do form (os que tem name) salva sozinho. Os campos
    // de digitacao do item nao tem name: so contam quando o item entra na grade.
    ['input', 'change'].forEach(ev => document.getElementById('formVenda').addEventListener(ev, e => {
        if (e.target.name && e.target.type !== 'hidden') agendarAutoSalvar();
    }));
    if (AUTO_SALVAR) statusAutoSalvar('<i class="bi bi-cloud-check me-1"></i>A venda é salva automaticamente', 'text-muted');
    telaIniciada = true;
});

