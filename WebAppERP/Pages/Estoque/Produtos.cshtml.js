// Script da tela Produtos (Pages/Estoque/Produtos.cshtml).

// ===== Busca de Produtos (filtros na pagina -> atualiza a grid via AJAX) =====
let resultadosBusca = [];

['buscaId', 'buscaTermo'].forEach(id => {
    document.getElementById(id)?.addEventListener('keydown', e => {
        if (e.key === 'Enter') { e.preventDefault(); buscarProdutos(); }
    });
});

function montarLinha(p, i) {
    const estoque = (p.estoque !== null && p.estoque !== undefined)
        ? parseFloat(p.estoque).toLocaleString('pt-BR', { maximumFractionDigits: 2 }) : '0';
    return '<tr>' +
        '<td>' + p.id + '</td>' +
        '<td><strong>' + escaparHtml(p.nome || '') + '</strong></td>' +
        '<td>' + escaparHtml(p.marca || '-') + '</td>' +
        '<td>' + escaparHtml(p.categoria || '-') + '</td>' +
        '<td>' + escaparHtml(p.unidade || '-') + '</td>' +
        '<td class="text-end">R$ ' + moeda(p.venda) + '</td>' +
        '<td class="text-end">' + estoque + '</td>' +
        '<td>' + statusBadge(p.ativo) + '</td>' +
        '<td class="text-center">' +
            '<button class="btn btn-icon btn-outline-primary" title="Editar" onclick="abrirEdicao(' + i + ')"><i class="bi bi-pencil"></i></button> ' +
            '<form method="post" action="?handler=Excluir" class="d-inline" onsubmit="return confirm(\'Deseja realmente excluir este produto?\')">' +
                campoToken() +
                '<input type="hidden" name="id" value="' + p.id + '">' +
                '<button type="submit" class="btn btn-icon btn-outline-danger" title="Excluir"><i class="bi bi-trash"></i></button>' +
            '</form>' +
        '</td>' +
    '</tr>';
}

async function buscarProdutos() {
    const tbody = document.querySelector('#tblProdutos tbody');
    const id = document.getElementById('buscaId').value.trim();
    const termo = document.getElementById('buscaTermo').value.trim();
    const inativos = document.getElementById('buscaInativos').checked;

    const params = new URLSearchParams();
    if (id) params.append('id', id);
    if (termo) params.append('termo', termo);
    params.append('incluirInativos', inativos);

    tbody.innerHTML = '<tr><td colspan="9" class="text-center text-muted py-3">Buscando...</td></tr>';
    try {
        const resp = await fetch('?handler=BuscarProdutos&' + params.toString());
        resultadosBusca = await resp.json();

        const contador = document.getElementById('contadorRegistros');
        if (contador) contador.textContent = resultadosBusca.length + ' registros encontrados';

        if (!resultadosBusca.length) {
            tbody.innerHTML = '<tr><td colspan="9" class="text-center text-muted py-4">Nenhum produto encontrado</td></tr>';
            return;
        }
        tbody.innerHTML = resultadosBusca.map((p, i) => montarLinha(p, i)).join('');
    } catch (e) {
        tbody.innerHTML = '<tr><td colspan="9" class="text-center text-danger py-4">Erro ao buscar. Tente novamente.</td></tr>';
    }
}

function abrirEdicao(i) {
    mostrarRegistro('modalProduto', resultadosBusca[i].registro);
    editarProdutoPorRegistro(resultadosBusca[i]);
    bootstrap.Modal.getOrCreateInstance(document.getElementById('modalProduto')).show();
}

