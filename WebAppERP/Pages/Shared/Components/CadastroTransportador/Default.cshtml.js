// Script do componente de cadastro de transportadora
// (Pages/Shared/Components/CadastroTransportador/Default.cshtml).
// Usado pela tela de Transportadores e pela pesquisa de transportadora (lupa do campo).

// Documento: CPF ou CNPJ conforme a quantidade de digitos (mascara do module.js).
aplicarMascara('trDoc', mascaraCpfCnpj);

function urlTransportadores(handler) {
    return document.getElementById('modalTransportador').dataset.urlTransportadores + '?handler=' + handler;
}

function limparFormTransportador() {
    document.getElementById('trTitulo').innerHTML = '<i class="bi bi-truck-front me-2"></i>Novo Transportador';
    document.getElementById('trId').value = '0';
    document.getElementById('trRazao').value = '';
    document.getElementById('trDoc').value = '';
    document.getElementById('trEndereco').value = '';
    limparCidade('trCidade');
    document.getElementById('trIE').value = '';
}

// cidadeNome opcional: sem ele o campo busca o nome pelo id.
function editarTransportador(id, razao, doc, end, cidade, ie, cidadeNome) {
    document.getElementById('trTitulo').innerHTML = '<i class="bi bi-pencil me-2"></i>Editar Transportador';
    document.getElementById('trId').value = id;
    document.getElementById('trRazao').value = razao || '';
    document.getElementById('trDoc').value = mascaraCpfCnpj(doc || '');
    document.getElementById('trEndereco').value = end || '';
    definirCidade('trCidade', cidade, cidadeNome);
    document.getElementById('trIE').value = ie || '';
}

// ===== Pesquisa de transportadora (a lupa do campo de transportadora) =====
// opcoes: o que a tela faz com o registro (ver abrirPesquisaComCadastro no module.js).
const PESQUISA_TRANSPORTADOR = {
    chave: 'transportador',
    titulo: 'Pesquisar Transportadora',
    // Mesma busca da tela de Transportadores (razao social ou CPF/CNPJ).
    url: () => urlTransportadores('BuscarTransportadores'),
    filtros: [
        { param: 'id', rotulo: 'ID', tipo: 'number', col: 2 },
        { param: 'termo', rotulo: 'Razão Social / CPF / CNPJ', col: 6 }
    ],
    colunas: ['Código', 'Razão Social', 'CPF / CNPJ', 'Cidade'],
    celulas: t => ['<strong>' + t.id + '</strong>', escaparHtml(t.nome),
                   t.documento ? mascaraCpfCnpj(t.documento) : '-', escaparHtml(t.cidadeNome || '-')],
    modal: 'modalTransportador',
    evento: 'transportador:salvo',
    preencher: t => editarTransportador(t.id, t.nome, t.documento, t.endereco, (t.idCidade ?? ''), t.inscEstadual, t.cidadeNome),
    limpar: limparFormTransportador,
    urlExcluir: () => urlTransportadores('Excluir'),
    nome: t => t.nome
};

function abrirPesquisaTransportador(opcoes) {
    abrirPesquisaComCadastro(PESQUISA_TRANSPORTADOR, opcoes);
}
