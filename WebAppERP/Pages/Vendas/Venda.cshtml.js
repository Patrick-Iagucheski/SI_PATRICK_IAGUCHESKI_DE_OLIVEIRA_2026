// Script da tela de Venda (inclusao e edicao) (Pages/Vendas/Venda.cshtml).
// Os valores que vem do servidor estao no bloco JSON #dadosTela da pagina.
const DADOS_TELA = JSON.parse(document.getElementById('dadosTela').textContent);

const PRODUTOS = DADOS_TELA.PRODUTOS;
const SERVICOS = DADOS_TELA.SERVICOS;
const SOMENTE_LEITURA = DADOS_TELA.SOMENTE_LEITURA;
let ITENS = DADOS_TELA.ITENS;

function moeda(n) { return (Number(n) || 0).toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }); }
function num(id) { const v = document.getElementById(id).value; return parseFloat(String(v).replace(',', '.')) || 0; }

function onTipoChange() {
    const t = document.getElementById('itTipo').value;
    document.getElementById('grpProduto').style.display = t === 'PRODUTO' ? '' : 'none';
    document.getElementById('grpServico').style.display = t === 'SERVICO' ? '' : 'none';
    document.getElementById('itVlUnit').value = '';
    document.getElementById('itTotal').value = '';
    document.getElementById('itSaldo').textContent = '';
}

function onProdutoChange() {
    const id = parseInt(document.getElementById('itProduto').value) || 0;
    const p = PRODUTOS.find(x => x.id === id);
    document.getElementById('itSaldo').textContent = p ? ('Saldo em estoque: ' + moeda(p.saldo)) : '';
    if (p) document.getElementById('itVlUnit').value = p.valor;
    calcItemTotal();
}

function onServicoChange() {
    const id = parseInt(document.getElementById('itServico').value) || 0;
    const s = SERVICOS.find(x => x.id === id);
    if (s) document.getElementById('itVlUnit').value = s.valor;
    calcItemTotal();
}

function calcItemTotal() {
    const total = num('itQt') * num('itVlUnit') - num('itDesc');
    document.getElementById('itTotal').value = (total > 0 ? total : 0).toFixed(2);
}

function adicionarItem() {
    const t = document.getElementById('itTipo').value;
    let item;
    if (t === 'PRODUTO') {
        const id = parseInt(document.getElementById('itProduto').value) || 0;
        const p = PRODUTOS.find(x => x.id === id);
        if (!p) { alert('Selecione um produto.'); return; }
        item = { tpItem: 'PRODUTO', idProduto: id, idServico: null, descricao: p.nome };
    } else {
        const id = parseInt(document.getElementById('itServico').value) || 0;
        const s = SERVICOS.find(x => x.id === id);
        if (!s) { alert('Selecione um servico.'); return; }
        item = { tpItem: 'SERVICO', idServico: id, idProduto: null, descricao: s.nome };
    }
    const qt = num('itQt');
    if (qt <= 0) { alert('Informe a quantidade.'); return; }
    const vlUnit = num('itVlUnit');
    const desc = num('itDesc');
    const total = qt * vlUnit - desc;
    item.qtItem = qt; item.vlUnitario = vlUnit; item.vlDesconto = desc; item.vlTotal = total > 0 ? total : 0;

    // Validacao de estoque (produto): considera o que ja esta no carrinho.
    if (item.tpItem === 'PRODUTO') {
        const p = PRODUTOS.find(x => x.id === item.idProduto);
        const noCarrinho = ITENS.filter(i => i.tpItem === 'PRODUTO' && i.idProduto === item.idProduto)
                                .reduce((a, b) => a + b.qtItem, 0);
        if (noCarrinho + qt > p.saldo) {
            if (!confirm('Quantidade acima do saldo em estoque (' + moeda(p.saldo) + '). A venda nao podera ser FINALIZADA assim. Adicionar mesmo assim?')) return;
        }
    }

    ITENS.push(item);
    limparItemForm();
    renderItens();
}

function limparItemForm() {
    document.getElementById('itProduto').value = '';
    document.getElementById('itServico').value = '';
    document.getElementById('itQt').value = '1';
    document.getElementById('itVlUnit').value = '';
    document.getElementById('itDesc').value = '0';
    document.getElementById('itTotal').value = '';
    document.getElementById('itSaldo').textContent = '';
}

function removerItem(i) { ITENS.splice(i, 1); renderItens(); }

function editarItem(i) {
    const it = ITENS[i];
    document.getElementById('itTipo').value = it.tpItem; onTipoChange();
    if (it.tpItem === 'PRODUTO') { document.getElementById('itProduto').value = it.idProduto; onProdutoChange(); }
    else { document.getElementById('itServico').value = it.idServico; }
    document.getElementById('itQt').value = it.qtItem;
    document.getElementById('itVlUnit').value = it.vlUnitario;
    document.getElementById('itDesc').value = it.vlDesconto;
    calcItemTotal();
    ITENS.splice(i, 1); renderItens();
}

function renderItens() {
    const tb = document.getElementById('itensBody');
    tb.innerHTML = '';
    if (ITENS.length === 0) {
        tb.innerHTML = '<tr><td colspan="8" class="text-center text-muted py-3">Nenhum item adicionado</td></tr>';
    }
    ITENS.forEach((it, idx) => {
        const codigo = it.tpItem === 'PRODUTO' ? it.idProduto : it.idServico;
        const badge = it.tpItem === 'PRODUTO' ? 'badge-agendado' : 'badge-confirmado';
        const acoes = SOMENTE_LEITURA ? '<td></td>' :
            '<td class="text-center" style="white-space:nowrap;">' +
            '<button type="button" class="btn btn-icon btn-outline-primary" title="Editar" onclick="editarItem(' + idx + ')"><i class="bi bi-pencil"></i></button> ' +
            '<button type="button" class="btn btn-icon btn-outline-danger" title="Remover" onclick="removerItem(' + idx + ')"><i class="bi bi-trash"></i></button></td>';
        const tr = document.createElement('tr');
        tr.innerHTML =
            '<td><span class="badge-status ' + badge + '">' + it.tpItem + '</span></td>' +
            '<td>' + codigo + '</td>' +
            '<td><strong>' + it.descricao + '</strong></td>' +
            '<td class="text-end">' + moeda(it.qtItem) + '</td>' +
            '<td class="text-end">R$ ' + moeda(it.vlUnitario) + '</td>' +
            '<td class="text-end">R$ ' + moeda(it.vlDesconto) + '</td>' +
            '<td class="text-end"><strong>R$ ' + moeda(it.vlTotal) + '</strong></td>' +
            acoes;
        tb.appendChild(tr);
    });
    calcTotais();
}

function calcTotais() {
    const subtotal = ITENS.reduce((a, b) => a + b.vlTotal, 0);
    const desc = num('VlDesconto');
    let total = subtotal - desc;
    if (total < 0) total = 0;
    document.getElementById('lblSubtotal').textContent = 'R$ ' + moeda(subtotal);
    document.getElementById('lblTotal').textContent = 'R$ ' + moeda(total);
}

function salvar(status) {
    const func = document.getElementById('IdFuncionario');
    if (!func.value) { alert('Selecione o funcionario responsavel.'); func.focus(); return; }
    if (status === 'FINALIZADA' && ITENS.length === 0) { alert('Adicione ao menos um item para finalizar.'); return; }
    document.getElementById('TpStatus').value = status;
    document.getElementById('ItensJson').value = JSON.stringify(ITENS.map(i => ({
        TpItem: i.tpItem, IdServico: i.idServico, IdProduto: i.idProduto,
        QtItem: i.qtItem, VlUnitario: i.vlUnitario, VlDesconto: i.vlDesconto
    })));
    document.getElementById('formVenda').submit();
}

document.addEventListener('DOMContentLoaded', function () {
    onTipoChange();
    renderItens();
    document.getElementById('VlDesconto')?.addEventListener('input', calcTotais);
    ['itQt', 'itVlUnit', 'itDesc'].forEach(id => document.getElementById(id)?.addEventListener('input', calcItemTotal));
});

