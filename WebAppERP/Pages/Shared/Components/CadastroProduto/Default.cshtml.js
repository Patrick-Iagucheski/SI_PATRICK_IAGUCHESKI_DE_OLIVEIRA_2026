// Script do componente de cadastro de produto
// (Pages/Shared/Components/CadastroProduto/Default.cshtml).
// Usado pela tela de Produtos e pela pesquisa de produto (lupa do item da
// Compra e da Venda). Nomes com sufixo "Produto" onde a tela (ou outro
// componente da mesma tela) ja tem uma funcao com o nome generico.

// ===== Preview / remocao da imagem =====
// Mostra o preview (arquivo novo escolhido ou imagem ja salva) com o botao de remover.
function mostrarImagem(src) {
    const wrap = document.getElementById('prodImgPreviewWrap');
    const img = document.getElementById('prodImgPreview');
    document.getElementById('prodImgIndisponivel').classList.add('d-none');
    img.classList.remove('d-none');
    img.src = src;
    wrap.classList.remove('d-none');
}

// Esconde totalmente o bloco de imagem (sem imagem atual nem preview).
function esconderImagem() {
    const wrap = document.getElementById('prodImgPreviewWrap');
    const img = document.getElementById('prodImgPreview');
    // removeAttribute em vez de src = '': src vazio dispara o onerror.
    img.removeAttribute('src');
    img.classList.remove('d-none');
    document.getElementById('prodImgIndisponivel').classList.add('d-none');
    wrap.classList.add('d-none');
}

// Chamado quando a imagem salva nao carrega (arquivo removido/inexistente):
// esconde o icone quebrado, avisa e mantem o botao de remover disponivel.
function imagemQuebrada() {
    document.getElementById('prodImgPreview').classList.add('d-none');
    document.getElementById('prodImgIndisponivel').classList.remove('d-none');
}

function previewImagem(e) {
    const file = e.target.files && e.target.files[0];
    if (!file) return;
    mostrarImagem(URL.createObjectURL(file));
}

// Remove a imagem: limpa o arquivo escolhido e o caminho salvo (hidden).
// Ao salvar o produto, o servidor apaga a imagem atual.
function removerImagem() {
    document.getElementById('prodImagemUpload').value = '';
    document.getElementById('prodDsImagem').value = '';
    esconderImagem();
}

// ===== Helpers AJAX =====
// Os handlers ficam em ProdutosModel; a URL vem do componente para
// funcionar em qualquer tela (e sob o diretorio virtual do IIS).
function urlProdutos(handler) {
    return document.getElementById('modalProduto').dataset.urlProdutos + '?handler=' + handler;
}

// ===== Marca e Categoria: pesquisa (a lupa do campo) e cadastro rapido =====
// Mesmo padrao das outras pesquisas: Selecionar coloca no campo do produto,
// Editar e Novo abrem o cadastro rapido, a lixeira usa o Excluir da tela de
// Marcas / Categorias (regras do Business).
const AUX_PRODUTO = {
    marca:     { nome: 'Marca',     plural: 'Marcas',     select: 'prodIdMarca',     pref: 'marca',     tela: 'urlMarcas',     busca: 'BuscarMarcas',     rapido: 'MarcaRapido',     dto: ['idMarca', 'nmMarca'] },
    categoria: { nome: 'Categoria', plural: 'Categorias', select: 'prodIdCategoria', pref: 'categoria', tela: 'urlCategorias', busca: 'BuscarCategorias', rapido: 'CategoriaRapido', dto: ['idCategoria', 'nmCategoria'] }
};

function urlTelaProduto(tela, handler) {
    return document.getElementById('modalProduto').dataset[tela] + '?handler=' + handler;
}

function abrirPesquisaAuxProduto(tipo) {
    const a = AUX_PRODUTO[tipo];
    abrirPesquisa({
        chave: tipo,
        titulo: 'Pesquisar ' + a.nome,
        url: () => urlTelaProduto(a.tela, a.busca),
        inativos: 'Exibir ' + a.plural + ' Inativas',
        filtros: [
            { param: 'id', rotulo: 'ID', tipo: 'number', col: 2 },
            { param: 'termo', rotulo: a.nome, col: 6 }
        ],
        colunas: ['Código', a.nome],
        celulas: r => ['<strong>' + r.id + '</strong>', escaparHtml(r.nome)],
        selecionar: r => selecionarNoSelect(a.select, r.id, r.nome),
        editar: r => abrirCadastroAuxProduto(tipo, r),
        novo: () => abrirCadastroAuxProduto(tipo, null),
        excluir: { url: () => urlTelaProduto(a.tela, 'Excluir'), nome: r => r.nome, depois: r => removerDoSelect(a.select, r.id) }
    });
}

function abrirCadastroAuxProduto(tipo, reg) {
    const a = AUX_PRODUTO[tipo];
    document.getElementById(a.pref + 'Msg').innerHTML = '';
    document.getElementById(a.pref + 'Titulo').textContent = (reg ? 'Editar ' : 'Cadastrar ') + a.nome;
    document.getElementById(a.pref + 'Id').value = reg ? reg.id : 0;
    document.getElementById(a.pref + 'Nome').value = reg ? reg.nome : '';
    abrirModalEmpilhado('modal' + a.nome + 'Rapido');
}

// Edicao volta para a pesquisa com a lista atualizada; cadastro novo ja fica
// selecionado no produto e fecha a pesquisa.
async function salvarAuxProduto(tipo) {
    const a = AUX_PRODUTO[tipo];
    const id = parseInt(document.getElementById(a.pref + 'Id').value) || 0;
    const nome = document.getElementById(a.pref + 'Nome').value.trim();
    const msg = document.getElementById(a.pref + 'Msg');
    const btn = document.getElementById('btnSalvar' + a.nome + 'Rapido');
    msg.innerHTML = '';
    if (!nome) { msg.innerHTML = alertaErro('Informe o nome da ' + a.nome.toLowerCase() + '.'); return; }

    btn.disabled = true;
    try {
        const data = await postJson(urlProdutos(a.rapido), { [a.dto[0]]: id, [a.dto[1]]: nome });
        if (!data.sucesso) { msg.innerHTML = alertaErro(data.mensagem); return; }
        if (id) renomearNoSelect(a.select, data.id, data.nome);
        concluirCadastro('modal' + a.nome + 'Rapido', tipo, id > 0, () => selecionarNoSelect(a.select, data.id, data.nome));
    } catch {
        msg.innerHTML = alertaErro('Erro ao salvar a ' + a.nome.toLowerCase() + '. Tente novamente.');
    } finally {
        btn.disabled = false;
    }
}

// ===== Modal (novo / editar) =====
function limparFormProduto() {
    document.getElementById('prodModalTitle').innerHTML = '<i class="bi bi-box-seam me-2"></i>Novo Produto';
    ['prodDsProduto','prodIdMarca','prodIdCategoria','prodSgUnidade','prodVlCusto','prodVlVendaMinimo','prodVlVenda','prodPcComissao','prodVlDesconto','prodQtSaldo','prodDsDescricao','prodCodigoDisplay','prodDsImagem','prodImagemUpload'].forEach(id => {
        document.getElementById(id).value = '';
    });
    document.getElementById('prodIdProduto').value = '0';
    definirAtivo('prodFlAtivo', true);
    esconderImagem();
}

// Normaliza numero removendo casas decimais sobrando (ex: "15.0000" -> 15)
function numProduto(v) {
    return (v === '' || v === null || v === undefined) ? '' : parseFloat(v);
}

function editarProduto(id, nome, idMarca, idCategoria, unidade, custo, vendaMin, venda, comissao, desconto, estoque, descricao, imagem, ativo) {
    document.getElementById('prodModalTitle').innerHTML = '<i class="bi bi-pencil me-2"></i>Editar Produto';
    document.getElementById('prodIdProduto').value = id;
    document.getElementById('prodCodigoDisplay').value = id;
    document.getElementById('prodDsProduto').value = nome;
    document.getElementById('prodIdMarca').value = idMarca || '';
    document.getElementById('prodIdCategoria').value = idCategoria || '';
    document.getElementById('prodSgUnidade').value = unidade || '';
    document.getElementById('prodVlCusto').value = numProduto(custo);
    document.getElementById('prodVlVendaMinimo').value = numProduto(vendaMin);
    document.getElementById('prodVlVenda').value = numProduto(venda);
    document.getElementById('prodPcComissao').value = numProduto(comissao);
    document.getElementById('prodVlDesconto').value = numProduto(desconto);
    document.getElementById('prodQtSaldo').value = numProduto(estoque);
    document.getElementById('prodDsDescricao').value = descricao || '';
    document.getElementById('prodDsImagem').value = imagem || '';
    document.getElementById('prodImagemUpload').value = '';
    definirAtivo('prodFlAtivo', ativo === 'True');

    if (imagem) { mostrarImagem(imagem); }
    else { esconderImagem(); }
}

// Registro da pesquisa / busca (BuscarProdutos) no formulario de edicao.
function editarProdutoPorRegistro(p) {
    editarProduto(p.id, p.nome || '', (p.idMarca ?? ''), (p.idCategoria ?? ''), p.unidade || '',
        (p.custo ?? ''), (p.vendaMin ?? ''), (p.venda ?? ''), (p.comissao ?? ''),
        (p.desconto ?? ''), (p.estoque ?? ''), p.descricao || '', p.imagem || '', (p.ativo ? 'True' : 'False'));
}

// ===== Pesquisa de produto (a lupa do campo de produto de outras telas) =====
// opcoes: o que a tela faz com o registro (ver abrirPesquisaComCadastro no module.js).
const PESQUISA_PRODUTO = {
    chave: 'produto',
    titulo: 'Pesquisar Produto',
    // Mesma busca da tela de Produtos (so ativos, sem diferenciar acento).
    url: () => urlProdutos('BuscarProdutos'),
    inativos: 'Exibir Produtos Inativos',
    filtros: [
        { param: 'id', rotulo: 'ID', tipo: 'number', col: 2 },
        { param: 'termo', rotulo: 'Produto / Marca / Categoria', col: 6 }
    ],
    colunas: ['Código', 'Produto', 'Marca', 'Categoria', 'Unidade', 'Custo', 'Venda', 'Estoque'],
    celulas: p => ['<strong>' + p.id + '</strong>', escaparHtml(p.nome), escaparHtml(p.marca || '-'),
                   escaparHtml(p.categoria || '-'), escaparHtml(p.unidade || '-'),
                   p.custo != null ? 'R$ ' + moeda(p.custo) : '-', p.venda != null ? 'R$ ' + moeda(p.venda) : '-',
                   p.estoque != null ? moeda(p.estoque) : '-'],
    modal: 'modalProduto',
    evento: 'produto:salvo',
    preencher: editarProdutoPorRegistro,
    limpar: limparFormProduto,
    urlExcluir: () => urlProdutos('Excluir'),
    nome: p => p.nome
};

function abrirPesquisaProduto(opcoes) {
    abrirPesquisaComCadastro(PESQUISA_PRODUTO, opcoes);
}
