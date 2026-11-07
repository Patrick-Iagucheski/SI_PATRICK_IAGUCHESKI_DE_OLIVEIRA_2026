// Script da tela Categorias (Pages/Estoque/Categorias.cshtml).

// ===== Busca de Categorias (filtros na pagina -> atualiza a grid via AJAX) =====
let resultadosBusca = [];

['buscaId', 'buscaTermo'].forEach(id => {
    document.getElementById(id)?.addEventListener('keydown', e => {
        if (e.key === 'Enter') { e.preventDefault(); buscarCategorias(); }
    });
});

function montarLinha(c, i) {
    return '<tr>' +
        '<td>' + c.id + '</td>' +
        '<td><strong>' + escaparHtml(c.nome || '') + '</strong></td>' +
        '<td>' + statusBadge(c.ativo) + '</td>' +
        '<td class="text-center">' +
            '<button class="btn btn-icon btn-outline-primary" title="Editar" onclick="abrirEdicao(' + i + ')"><i class="bi bi-pencil"></i></button> ' +
            '<form method="post" action="?handler=Excluir" class="d-inline" onsubmit="return confirm(\'Deseja realmente excluir esta categoria?\')">' +
                campoToken() +
                '<input type="hidden" name="id" value="' + c.id + '">' +
                '<button type="submit" class="btn btn-icon btn-outline-danger" title="Excluir"><i class="bi bi-trash"></i></button>' +
            '</form>' +
        '</td>' +
    '</tr>';
}

async function buscarCategorias() {
    const tbody = document.querySelector('#tblCategorias tbody');
    const id = document.getElementById('buscaId').value.trim();
    const termo = document.getElementById('buscaTermo').value.trim();
    const inativos = document.getElementById('buscaInativos').checked;

    const params = new URLSearchParams();
    if (id) params.append('id', id);
    if (termo) params.append('termo', termo);
    params.append('incluirInativos', inativos);

    tbody.innerHTML = '<tr><td colspan="4" class="text-center text-muted py-3">Buscando...</td></tr>';
    try {
        const resp = await fetch('?handler=BuscarCategorias&' + params.toString());
        resultadosBusca = await resp.json();

        const contador = document.getElementById('contadorRegistros');
        if (contador) contador.textContent = resultadosBusca.length + ' registros encontrados';

        if (!resultadosBusca.length) {
            tbody.innerHTML = '<tr><td colspan="4" class="text-center text-muted py-4">Nenhuma categoria encontrada</td></tr>';
            return;
        }
        tbody.innerHTML = resultadosBusca.map((c, i) => montarLinha(c, i)).join('');
    } catch (e) {
        tbody.innerHTML = '<tr><td colspan="4" class="text-center text-danger py-4">Erro ao buscar. Tente novamente.</td></tr>';
    }
}

function abrirEdicao(i) {
    mostrarRegistro('modalCategoria', resultadosBusca[i].registro);
    const c = resultadosBusca[i];
    editarCategoria(c.id, c.nome || '', (c.ativo ? 'True' : 'False'));
    bootstrap.Modal.getOrCreateInstance(document.getElementById('modalCategoria')).show();
}

// ===== Modal (novo / editar) =====
function limparForm() {
    document.getElementById('modalTitle').innerHTML = '<i class="bi bi-collection me-2"></i>Nova Categoria';
    document.getElementById('cId').value = '0';
    document.getElementById('cCodigoDisplay').value = '';
    document.getElementById('cNome').value = '';
    definirAtivo('cAtivo', true);
}

function editarCategoria(id, nome, ativo) {
    document.getElementById('modalTitle').innerHTML = '<i class="bi bi-pencil me-2"></i>Editar Categoria';
    document.getElementById('cId').value = id;
    document.getElementById('cCodigoDisplay').value = id;
    document.getElementById('cNome').value = nome;
    definirAtivo('cAtivo', ativo === 'True');
}

