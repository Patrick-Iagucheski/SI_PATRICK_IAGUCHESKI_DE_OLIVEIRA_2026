// Script do componente de cadastro de produto
// (Pages/Shared/Components/CadastroProduto/Default.cshtml).
// Usado pela tela de Produtos e pela pesquisa de produto da Compra.
// Nomes com sufixo "Produto" onde a Compra (ou o componente de fornecedor,
// que fica na mesma tela) ja tem uma funcao com o nome generico.

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
function tokenProduto() {
    return document.querySelector('#modalProduto input[name="__RequestVerificationToken"]').value;
}
function alertaErroProduto(m) {
    return '<div class="alert alert-danger py-1 px-2 small mb-2">' + m + '</div>';
}
async function postJsonProduto(url, body) {
    const resp = await fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': tokenProduto() },
        body: JSON.stringify(body)
    });
    return await resp.json();
}

// ===== Cadastro rapido de Marca (AJAX) =====
async function salvarMarcaRapida() {
    const nome = document.getElementById('marcaNome').value.trim();
    const msg = document.getElementById('marcaMsg');
    const btn = document.getElementById('btnSalvarMarcaRapido');
    msg.innerHTML = '';

    if (!nome) { msg.innerHTML = alertaErroProduto('Informe o nome da marca.'); return; }

    btn.disabled = true;
    try {
        const data = await postJsonProduto(urlProdutos('MarcaRapido'), { nmMarca: nome });
        if (!data.sucesso) { msg.innerHTML = alertaErroProduto(data.mensagem); return; }

        // Adiciona a nova marca no dropdown e ja a seleciona automaticamente.
        const sel = document.getElementById('prodIdMarca');
        const opt = document.createElement('option');
        opt.value = data.id;
        opt.textContent = data.nome;
        sel.appendChild(opt);
        sel.value = data.id;

        document.getElementById('marcaNome').value = '';
        bootstrap.Modal.getInstance(document.getElementById('modalMarcaRapido')).hide();
    } catch (e) {
        msg.innerHTML = alertaErroProduto('Erro ao salvar a marca. Tente novamente.');
    } finally {
        btn.disabled = false;
    }
}

// ===== Cadastro rapido de Categoria (AJAX) =====
async function salvarCategoriaRapida() {
    const nome = document.getElementById('categoriaNome').value.trim();
    const msg = document.getElementById('categoriaMsg');
    const btn = document.getElementById('btnSalvarCategoriaRapido');
    msg.innerHTML = '';

    if (!nome) { msg.innerHTML = alertaErroProduto('Informe o nome da categoria.'); return; }

    btn.disabled = true;
    try {
        const data = await postJsonProduto(urlProdutos('CategoriaRapido'), { nmCategoria: nome });
        if (!data.sucesso) { msg.innerHTML = alertaErroProduto(data.mensagem); return; }

        // Adiciona a nova categoria no dropdown e ja a seleciona automaticamente.
        const sel = document.getElementById('prodIdCategoria');
        const opt = document.createElement('option');
        opt.value = data.id;
        opt.textContent = data.nome;
        sel.appendChild(opt);
        sel.value = data.id;

        document.getElementById('categoriaNome').value = '';
        bootstrap.Modal.getInstance(document.getElementById('modalCategoriaRapido')).hide();
    } catch (e) {
        msg.innerHTML = alertaErroProduto('Erro ao salvar a categoria. Tente novamente.');
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
    document.getElementById('prodFlAtivo').value = 'true';
    document.getElementById('prodMsg').innerHTML = '';
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
    document.getElementById('prodFlAtivo').value = ativo === 'True' ? 'true' : 'false';
    document.getElementById('prodMsg').innerHTML = '';

    if (imagem) { mostrarImagem(imagem); }
    else { esconderImagem(); }
}

// ===== Modo AJAX: grava sem recarregar a tela que abriu o cadastro =====
document.querySelector('#modalProduto form')?.addEventListener('submit', function (e) {
    if (document.getElementById('modalProduto').dataset.modo !== 'ajax') return;
    e.preventDefault();
    salvarProdutoAjax(this);
});

// Grava o produto (com a imagem, se houver) e avisa a tela de origem pelo
// evento 'produto:salvo'.
async function salvarProdutoAjax(form) {
    const msg = document.getElementById('prodMsg');
    const btn = form.querySelector('button[type="submit"]');
    msg.innerHTML = '';
    btn.disabled = true;
    try {
        const resp = await fetch(urlProdutos('SalvarAjax'), { method: 'POST', body: new FormData(form) });
        const data = await resp.json();
        if (!data.sucesso) { msg.innerHTML = alertaErroProduto(data.mensagem); msg.scrollIntoView({ block: 'nearest' }); return; }
        bootstrap.Modal.getInstance(document.getElementById('modalProduto')).hide();
        document.dispatchEvent(new CustomEvent('produto:salvo', { detail: data }));
    } catch (e) {
        msg.innerHTML = alertaErroProduto('Erro ao salvar o produto. Tente novamente.');
        msg.scrollIntoView({ block: 'nearest' });
    } finally { btn.disabled = false; }
}
