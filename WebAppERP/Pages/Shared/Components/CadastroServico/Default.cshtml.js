// Script do componente de cadastro de servico
// (Pages/Shared/Components/CadastroServico/Default.cshtml).
// Usado pela tela de Servicos e pelas pesquisas de servico (Compra, Venda, Agendamentos).

function urlServicos(handler) {
    return document.getElementById('modalServico').dataset.urlCadastro + '?handler=' + handler;
}

function limparFormServico() {
    document.getElementById('svTitulo').innerHTML = '<i class="bi bi-card-checklist me-2"></i>Novo Serviço';
    document.getElementById('sId').value = '0';
    document.getElementById('sNome').value = '';
    document.getElementById('sDesc').value = '';
    document.getElementById('sValor').value = '';
    document.getElementById('sDuracao').value = '';
    document.getElementById('sCat').value = '';
    definirAtivo('sAtivo', true);
}

function editarServico(id, nome, desc, valor, duracao, cat, ativo) {
    document.getElementById('svTitulo').innerHTML = '<i class="bi bi-pencil me-2"></i>Editar Serviço';
    document.getElementById('sId').value = id;
    document.getElementById('sNome').value = nome || '';
    document.getElementById('sDesc').value = desc || '';
    document.getElementById('sValor').value = valor ?? '';
    document.getElementById('sDuracao').value = duracao ?? '';
    document.getElementById('sCat').value = cat || '';
    definirAtivo('sAtivo', ativo === true || ativo === 'True');
}

// ===== Pesquisa de servico (a lupa do campo de servico de outras telas) =====
// opcoes: o que a tela faz com o registro (ver abrirPesquisaComCadastro no module.js).
const PESQUISA_SERVICO = {
    chave: 'servico',
    titulo: 'Pesquisar Serviço',
    // Mesma busca da tela de Servicos (so ativos, sem diferenciar acento).
    url: () => urlServicos('BuscarServicos'),
    inativos: 'Exibir Serviços Inativos',
    filtros: [
        { param: 'id', rotulo: 'ID', tipo: 'number', col: 2 },
        { param: 'termo', rotulo: 'Serviço / Categoria', col: 6 }
    ],
    colunas: ['Código', 'Serviço', 'Categoria', 'Duração', 'Valor'],
    celulas: s => ['<strong>' + s.id + '</strong>', escaparHtml(s.nome), escaparHtml(s.categoria || '-'),
                   s.duracao ? s.duracao + ' min' : '-', 'R$ ' + moeda(s.valor)],
    modal: 'modalServico',
    evento: 'servico:salvo',
    preencher: s => editarServico(s.id, s.nome, s.descricao, s.valor, s.duracao, s.categoria, s.ativo),
    limpar: limparFormServico,
    urlExcluir: () => urlServicos('Excluir'),
    nome: s => s.nome
};

function abrirPesquisaServico(opcoes) {
    abrirPesquisaComCadastro(PESQUISA_SERVICO, opcoes);
}
