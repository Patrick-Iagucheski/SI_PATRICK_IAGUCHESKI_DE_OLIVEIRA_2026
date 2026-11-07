// Script do componente de cadastro de forma de pagamento
// (Pages/Shared/Components/CadastroFormaPagamento/Default.cshtml).
// Usado pela tela de Formas de Pagamento e pela pesquisa de forma (lupa dos
// campos de forma da Venda, da Compra e de Contas a Pagar).

function urlFormas(handler) {
    return document.getElementById('modalForma').dataset.urlCadastro + '?handler=' + handler;
}

function limparFormForma() {
    document.getElementById('fpTitulo').innerHTML = '<i class="bi bi-credit-card me-2"></i>Nova Forma de Pagamento';
    document.getElementById('fpId').value = '0';
    document.getElementById('fpNome').value = '';
    definirAtivo('fpAtivo', true);
}

function editarForma(id, desc, ativo) {
    document.getElementById('fpTitulo').innerHTML = '<i class="bi bi-pencil me-2"></i>Editar Forma de Pagamento';
    document.getElementById('fpId').value = id;
    document.getElementById('fpNome').value = desc || '';
    definirAtivo('fpAtivo', ativo === true || ativo === 'True');
}

// ===== Pesquisa de forma de pagamento (a lupa do campo de forma) =====
// opcoes: o que a tela faz com o registro (ver abrirPesquisaComCadastro no module.js).
const PESQUISA_FORMA = {
    chave: 'forma',
    titulo: 'Pesquisar Forma de Pagamento',
    // So ativas, sem diferenciar acento.
    url: () => urlFormas('BuscarFormas'),
    inativos: 'Exibir Formas Inativas',
    filtros: [
        { param: 'id', rotulo: 'ID', tipo: 'number', col: 2 },
        { param: 'termo', rotulo: 'Descrição', col: 6 }
    ],
    colunas: ['Código', 'Descrição'],
    celulas: f => ['<strong>' + f.id + '</strong>', escaparHtml(f.nome)],
    modal: 'modalForma',
    evento: 'forma:salva',
    preencher: f => editarForma(f.id, f.nome, f.ativo),
    limpar: limparFormForma,
    urlExcluir: () => urlFormas('Excluir'),
    nome: f => f.nome
};

function abrirPesquisaForma(opcoes) {
    abrirPesquisaComCadastro(PESQUISA_FORMA, opcoes);
}
