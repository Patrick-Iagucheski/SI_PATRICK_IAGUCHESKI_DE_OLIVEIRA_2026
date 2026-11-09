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

// ===== Marca, Categoria e Unidade de Medida: pesquisa (o + do campo) e cadastro rapido =====
// Mesmo padrao das outras pesquisas: Selecionar coloca no campo do produto,
// Editar e Novo abrem o cadastro rapido, a lixeira usa o Excluir da tela de
// Marcas / Categorias / Unidades de Medida (regras do Business).
// Opcionais de cada tipo:
//   sigla:  propriedade do campo Sigla no envio (a unidade tem sigla e descricao;
//           o campo da tela e pref + 'Sigla')
//   filtro: rotulo do filtro de texto da pesquisa (padrao: nome)
//   campo:  como o campo nome e chamado na mensagem de obrigatorio (padrao: nome)
//   texto:  texto do registro no campo do produto (padrao: r.nome)
const AUX_PRODUTO = {
    marca:     { nome: 'Marca',     plural: 'Marcas',     select: 'prodIdMarca',     pref: 'marca',     tela: 'urlMarcas',     busca: 'BuscarMarcas',     rapido: 'MarcaRapido',     dto: ['idMarca', 'nmMarca'] },
    categoria: { nome: 'Categoria', titulo: 'Categoria de Produto', plural: 'Categorias', select: 'prodIdCategoria', pref: 'categoria', tela: 'urlCategorias', busca: 'BuscarCategorias', rapido: 'CategoriaRapido', dto: ['idCategoria', 'nmCategoria'] },
    // Unidade: o campo mostra "SIGLA - Descricao" (r.descricao)
    unidade:   { nome: 'Unidade',   titulo: 'Unidade de Medida', plural: 'Unidades', select: 'prodIdUnidadeMedida', pref: 'unidade', tela: 'urlUnidades', busca: 'BuscarUnidades', rapido: 'UnidadeRapido', dto: ['idUnidadeMedida', 'dsUnidade'],
                 sigla: 'sgUnidade', filtro: 'Sigla / Descrição', campo: 'a descrição', texto: r => r.descricao }
};

function urlTelaProduto(tela, handler) {
    return document.getElementById('modalProduto').dataset[tela] + '?handler=' + handler;
}

function abrirPesquisaAuxProduto(tipo) {
    const a = AUX_PRODUTO[tipo];
    const texto = a.texto || (r => r.nome);
    abrirPesquisa({
        chave: tipo,
        titulo: 'Pesquisar ' + (a.titulo || a.nome),
        url: () => urlTelaProduto(a.tela, a.busca),
        inativos: 'Exibir ' + a.plural + ' Inativas',
        filtros: [
            { param: 'id', rotulo: 'ID', tipo: 'number', col: 2 },
            { param: 'termo', rotulo: a.filtro || a.nome, col: 6 }
        ],
        colunas: a.sigla ? ['Código', 'Sigla', 'Descrição'] : ['Código', a.nome],
        celulas: r => a.sigla
            ? ['<strong>' + r.id + '</strong>', escaparHtml(r.sigla), escaparHtml(r.nome)]
            : ['<strong>' + r.id + '</strong>', escaparHtml(r.nome)],
        selecionar: r => selecionarNoSelect(a.select, r.id, texto(r)),
        editar: r => abrirCadastroAuxProduto(tipo, r),
        novo: () => abrirCadastroAuxProduto(tipo, null),
        excluir: { url: () => urlTelaProduto(a.tela, 'Excluir'), nome: texto, depois: r => removerDoSelect(a.select, r.id) }
    });
}

function abrirCadastroAuxProduto(tipo, reg) {
    const a = AUX_PRODUTO[tipo];
    document.getElementById(a.pref + 'Msg').innerHTML = '';
    document.getElementById(a.pref + 'Titulo').textContent = (reg ? 'Editar ' : 'Cadastrar ') + (a.titulo || a.nome);
    document.getElementById(a.pref + 'Id').value = reg ? reg.id : 0;
    if (a.sigla) document.getElementById(a.pref + 'Sigla').value = reg ? reg.sigla : '';
    document.getElementById(a.pref + 'Nome').value = reg ? reg.nome : '';
    mostrarRegistro('modal' + a.nome + 'Rapido', reg?.registro);
    abrirModalEmpilhado('modal' + a.nome + 'Rapido');
}

// Edicao volta para a pesquisa com a lista atualizada; cadastro novo ja fica
// selecionado no produto e fecha a pesquisa.
async function salvarAuxProduto(tipo) {
    const a = AUX_PRODUTO[tipo];
    const id = parseInt(document.getElementById(a.pref + 'Id').value) || 0;
    const sigla = a.sigla ? document.getElementById(a.pref + 'Sigla').value.trim() : null;
    const nome = document.getElementById(a.pref + 'Nome').value.trim();
    const msg = document.getElementById(a.pref + 'Msg');
    const btn = document.getElementById('btnSalvar' + a.nome + 'Rapido');
    msg.innerHTML = '';
    if (a.sigla && !sigla) { msg.innerHTML = alertaErro('Informe a sigla da ' + a.nome.toLowerCase() + '.'); return; }
    if (!nome) { msg.innerHTML = alertaErro('Informe ' + (a.campo || 'o nome') + ' da ' + a.nome.toLowerCase() + '.'); return; }

    const dados = { [a.dto[0]]: id, [a.dto[1]]: nome };
    if (a.sigla) dados[a.sigla] = sigla;

    btn.disabled = true;
    try {
        const data = await postJson(urlProdutos(a.rapido), dados);
        if (!data.sucesso) { msg.innerHTML = alertaErro(data.mensagem); return; }
        if (id) renomearNoSelect(a.select, data.id, data.nome);
        concluirCadastro('modal' + a.nome + 'Rapido', tipo, id > 0, () => selecionarNoSelect(a.select, data.id, data.nome));
    } catch {
        msg.innerHTML = alertaErro('Erro ao salvar a ' + (a.titulo || a.nome).toLowerCase() + '. Tente novamente.');
    } finally {
        btn.disabled = false;
    }
}

// ===== Modal (novo / editar) =====
function limparFormProduto() {
    document.getElementById('prodModalTitle').innerHTML = '<i class="bi bi-box-seam me-2"></i>Novo Produto';
    ['prodDsProduto','prodIdMarca','prodIdCategoria','prodIdUnidadeMedida','prodVlVenda','prodPcComissao','prodDsDescricao','prodCodigoDisplay','prodDsImagem','prodImagemUpload'].forEach(id => {
        document.getElementById(id).value = '';
    });
    document.getElementById('prodIdProduto').value = '0';
    definirAtivo('prodFlAtivo', true);
    mostrarEntradaProduto(null);
    esconderImagem();
}

// Custo medio e estoque (so leitura): aparecem so quando o produto ja teve
// entrada por compra finalizada.
function mostrarEntradaProduto(p) {
    const tem = !!p?.temEntrada;
    document.getElementById('prodSecaoEstoque').classList.toggle('d-none', !tem);
    // Custo medio com 6 casas, como e calculado na compra.
    document.getElementById('prodCustoDisplay').value = tem
        ? 'R$ ' + moeda(p.custo, 6) : '';
    document.getElementById('prodEstoqueDisplay').value = tem
        ? (Number(p.estoque) || 0).toLocaleString('pt-BR', { maximumFractionDigits: 2 }) + (p.unidade ? ' ' + p.unidade : '') : '';
}

// Normaliza numero removendo casas decimais sobrando (ex: "15.0000" -> 15)
function numProduto(v) {
    return (v === '' || v === null || v === undefined) ? '' : parseFloat(v);
}

// p: registro do produto (ProdutosModel.Registro: busca, pesquisa e linhas da tela).
function editarProduto(p) {
    document.getElementById('prodModalTitle').innerHTML = '<i class="bi bi-pencil me-2"></i>Editar Produto';
    document.getElementById('prodIdProduto').value = p.id;
    document.getElementById('prodCodigoDisplay').value = p.id;
    document.getElementById('prodDsProduto').value = p.nome || '';
    document.getElementById('prodIdMarca').value = p.idMarca || '';
    document.getElementById('prodIdCategoria').value = p.idCategoria || '';
    if (p.idUnidade) selecionarNoSelect('prodIdUnidadeMedida', p.idUnidade, p.unidadeDescricao || p.unidade || String(p.idUnidade));
    else document.getElementById('prodIdUnidadeMedida').value = '';
    document.getElementById('prodVlVenda').value = numProduto(p.venda);
    document.getElementById('prodPcComissao').value = numProduto(p.comissao);
    document.getElementById('prodDsDescricao').value = p.descricao || '';
    document.getElementById('prodDsImagem').value = p.imagem || '';
    document.getElementById('prodImagemUpload').value = '';
    definirAtivo('prodFlAtivo', p.ativo);
    mostrarEntradaProduto(p);

    if (p.imagem) { mostrarImagem(p.imagem); }
    else { esconderImagem(); }
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
                   p.custo != null ? 'R$ ' + moeda(p.custo, 6) : '-', p.venda != null ? 'R$ ' + moeda(p.venda) : '-',
                   p.estoque != null ? moeda(p.estoque) : '-'],
    modal: 'modalProduto',
    evento: 'produto:salvo',
    preencher: editarProduto,
    limpar: limparFormProduto,
    urlExcluir: () => urlProdutos('Excluir'),
    nome: p => p.nome
};

function abrirPesquisaProduto(opcoes) {
    abrirPesquisaComCadastro(PESQUISA_PRODUTO, opcoes);
}
