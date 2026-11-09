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

function editarServico(id, nome, desc, valor, duracao, idCategoria, ativo) {
    document.getElementById('svTitulo').innerHTML = '<i class="bi bi-pencil me-2"></i>Editar Serviço';
    document.getElementById('sId').value = id;
    document.getElementById('sNome').value = nome || '';
    document.getElementById('sDesc').value = desc || '';
    document.getElementById('sValor').value = valor ?? '';
    document.getElementById('sDuracao').value = duracao ?? '';
    document.getElementById('sCat').value = idCategoria || '';
    definirAtivo('sAtivo', ativo === true || ativo === 'True');
}

// ===== Categoria de servico: pesquisa (o + do campo) e cadastro rapido =====
// Mesmo padrao de marca / categoria do produto; a lixeira usa o Excluir da
// tela de Categorias de Servico.
function urlCategoriasServico(handler) {
    return document.getElementById('modalServico').dataset.urlCategorias + '?handler=' + handler;
}

function abrirPesquisaCategoriaServico() {
    abrirPesquisa({
        chave: 'categoriaServico',
        titulo: 'Pesquisar Categoria de Serviço',
        url: () => urlCategoriasServico('BuscarCategorias'),
        inativos: 'Exibir Categorias Inativas',
        filtros: [
            { param: 'id', rotulo: 'ID', tipo: 'number', col: 2 },
            { param: 'termo', rotulo: 'Categoria', col: 6 }
        ],
        colunas: ['Código', 'Categoria'],
        celulas: c => ['<strong>' + c.id + '</strong>', escaparHtml(c.nome)],
        selecionar: c => selecionarNoSelect('sCat', c.id, c.nome),
        editar: c => abrirCadastroCategoriaServico(c),
        novo: () => abrirCadastroCategoriaServico(null),
        excluir: { url: () => urlCategoriasServico('Excluir'), nome: c => c.nome, depois: c => removerDoSelect('sCat', c.id) }
    });
}

function abrirCadastroCategoriaServico(c) {
    document.getElementById('catServMsg').innerHTML = '';
    document.getElementById('catServTitulo').textContent = c ? 'Editar Categoria de Serviço' : 'Cadastrar Categoria de Serviço';
    document.getElementById('catServId').value = c ? c.id : 0;
    document.getElementById('catServNome').value = c ? c.nome : '';
    mostrarRegistro('modalCategoriaServicoRapido', c?.registro);
    abrirModalEmpilhado('modalCategoriaServicoRapido');
}

async function salvarCategoriaServico() {
    const id = parseInt(document.getElementById('catServId').value) || 0;
    const nome = document.getElementById('catServNome').value.trim();
    const msg = document.getElementById('catServMsg');
    const btn = document.getElementById('btnSalvarCategoriaServicoRapido');
    msg.innerHTML = '';
    if (!nome) { msg.innerHTML = alertaErro('Informe o nome da categoria.'); return; }

    btn.disabled = true;
    try {
        const data = await postJson(urlServicos('CategoriaRapido'), { idCategoriaServico: id, nmCategoria: nome });
        if (!data.sucesso) { msg.innerHTML = alertaErro(data.mensagem); return; }
        if (id) renomearNoSelect('sCat', data.id, data.nome);
        concluirCadastro('modalCategoriaServicoRapido', 'categoriaServico', id > 0, () => selecionarNoSelect('sCat', data.id, data.nome));
    } catch {
        msg.innerHTML = alertaErro('Erro ao salvar a categoria. Tente novamente.');
    } finally {
        btn.disabled = false;
    }
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
    preencher: s => editarServico(s.id, s.nome, s.descricao, s.valor, s.duracao, s.idCategoria, s.ativo),
    limpar: limparFormServico,
    urlExcluir: () => urlServicos('Excluir'),
    nome: s => s.nome
};

function abrirPesquisaServico(opcoes) {
    abrirPesquisaComCadastro(PESQUISA_SERVICO, opcoes);
}
