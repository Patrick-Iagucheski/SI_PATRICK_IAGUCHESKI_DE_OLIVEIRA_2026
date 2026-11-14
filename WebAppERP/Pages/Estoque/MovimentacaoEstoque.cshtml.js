// Script da tela de Movimentacao de Estoque (Pages/Estoque/MovimentacaoEstoque.cshtml).
// Os valores que vem do servidor estao no bloco JSON #dadosTela da pagina.
const DADOS_TELA = JSON.parse(document.getElementById('dadosTela').textContent);
const PRODUTOS = DADOS_TELA.PRODUTOS;

function novoLancamento() {
    const form = document.getElementById('formLancamento');
    form.reset();
    exibirErro(form, null);
    document.getElementById('lancIdProduto').value = '';
    prepararCusto();
    atualizarPrevia();
}

// Pesquisa do produto (componente de cadastro), guardando saldo e unidade na
// lista local para a previa do saldo.
function abrirBuscaProdutoLancamento() {
    abrirPesquisaProduto(opcoesDoSelectComLista('lancIdProduto', PRODUTOS,
        p => ({ id: p.id, nome: p.nome, saldo: p.estoque ?? 0, custo: p.custo, unidade: p.unidade })));
}

function produtoEscolhido() {
    const id = parseInt(document.getElementById('lancIdProduto').value) || 0;
    return PRODUTOS.find(p => p.id === id);
}

// Saldo que o produto vai ficar: entrada soma; saida e perda tiram.
function saldoDepois() {
    const p = produtoEscolhido();
    if (!p) return null;
    const qt = parseFloat(document.getElementById('lancQuantidade').value) || 0;
    const entrada = ehEntrada();
    return (Number(p.saldo) || 0) + (entrada ? qt : -qt);
}

function ehEntrada() { return document.getElementById('lancTipo').value === DADOS_TELA.ENTRADA; }

// Custo unitario so na entrada: obrigatorio, sugerido com o custo medio atual.
// Saida e perda escondem o campo (desabilitado, nao vai no post).
function prepararCusto() {
    const p = produtoEscolhido();
    const campo = document.getElementById('lancCusto');
    const entrada = ehEntrada();
    document.getElementById('lancWrapCusto').classList.toggle('d-none', !entrada);
    campo.disabled = !entrada;
    campo.required = entrada;
    campo.value = entrada && p?.custo != null ? Number(p.custo).toFixed(2) : '';
}

function atualizarPrevia() {
    const p = produtoEscolhido();
    const apos = saldoDepois();
    document.getElementById('lancUnidade').textContent = p?.unidade || 'UN';
    document.getElementById('lancSaldoAtual').value = p ? moeda(p.saldo) : '';
    const campo = document.getElementById('lancSaldoApos');
    campo.value = apos === null ? '' : moeda(apos);
    campo.classList.toggle('text-danger', apos !== null && apos < 0);
    campo.classList.toggle('text-success', apos !== null && apos >= 0);
}

document.addEventListener('DOMContentLoaded', function () {
    ['lancIdProduto', 'lancTipo'].forEach(id => document.getElementById(id).addEventListener('change', () => {
        prepararCusto();
        atualizarPrevia();
    }));
    document.getElementById('lancQuantidade').addEventListener('input', atualizarPrevia);

    // Saida ou perda que deixa o saldo negativo: confirma antes de gravar.
    // Roda antes do envio por AJAX (module.js), que respeita o preventDefault.
    document.getElementById('formLancamento').addEventListener('submit', e => {
        const apos = saldoDepois();
        if (apos !== null && apos < 0
            && !confirm('O saldo de ' + produtoEscolhido().nome + ' vai ficar negativo (' + moeda(apos) + '). Confirmar o lançamento?'))
            e.preventDefault();
    });
});
