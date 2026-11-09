// Script da tela Categorias de Servico (Pages/Gerais/CategoriasServico.cshtml).

// ===== Busca (filtros na pagina -> atualiza a grade via AJAX) =====
let resultadosBusca = [];

['buscaId', 'buscaTermo'].forEach(id => {
    document.getElementById(id)?.addEventListener('keydown', e => {
        if (e.key === 'Enter') { e.preventDefault(); buscarCategorias(); }
    });
});

function montarLinha(c, i) {
    return '<tr>' +
        '<td>' + c.id + '</td>' +
        '<td><strong>' + escaparHtml(c.nome) + '</strong></td>' +
        '<td>' + statusBadge(c.ativo) + '</td>' +
        '<td class="text-center">' +
            '<button class="btn btn-icon btn-outline-primary" title="Editar" onclick="abrirEdicao(' + i + ')"><i class="bi bi-pencil"></i></button> ' +
            '<form method="post" action="?handler=Excluir" class="d-inline" onsubmit="return confirm(\'Deseja realmente excluir esta categoria de serviço?\')">' +
                campoToken() +
                '<input type="hidden" name="id" value="' + c.id + '">' +
                '<button type="submit" class="btn btn-icon btn-outline-danger" title="Excluir"><i class="bi bi-trash"></i></button>' +
            '</form>' +
        '</td>' +
    '</tr>';
}

async function buscarCategorias() {
    const tbody = document.querySelector('#tblCategorias tbody');
    const params = new URLSearchParams();
    const id = document.getElementById('buscaId').value.trim();
    const termo = document.getElementById('buscaTermo').value.trim();
    if (id) params.append('id', id);
    if (termo) params.append('termo', termo);
    params.append('incluirInativos', document.getElementById('buscaInativos').checked);

    tbody.innerHTML = '<tr><td colspan="4" class="text-center text-muted py-3">Buscando...</td></tr>';
    try {
        resultadosBusca = await (await fetch('?handler=BuscarCategorias&' + params.toString())).json();
        document.getElementById('contadorRegistros').textContent = resultadosBusca.length + ' registros encontrados';
        tbody.innerHTML = resultadosBusca.length
            ? resultadosBusca.map((c, i) => montarLinha(c, i)).join('')
            : '<tr><td colspan="4" class="text-center text-muted py-4">Nenhuma categoria de serviço encontrada</td></tr>';
    } catch {
        tbody.innerHTML = '<tr><td colspan="4" class="text-center text-danger py-4">Erro ao buscar. Tente novamente.</td></tr>';
    }
}

function abrirEdicao(i) {
    mostrarRegistro('modalCategoriaServico', resultadosBusca[i].registro);
    editarCategoriaServico(resultadosBusca[i]);
    bootstrap.Modal.getOrCreateInstance(document.getElementById('modalCategoriaServico')).show();
}

// ===== Modal (novo / editar) =====
function limparFormCategoriaServico() {
    document.getElementById('csTitulo').innerHTML = '<i class="bi bi-bookmarks me-2"></i>Nova Categoria de Serviço';
    document.getElementById('csId').value = '0';
    document.getElementById('csCodigoDisplay').value = '';
    document.getElementById('csNome').value = '';
    definirAtivo('csAtivo', true);
}

// c: registro da busca (CategoriasServicoModel.Registro).
function editarCategoriaServico(c) {
    document.getElementById('csTitulo').innerHTML = '<i class="bi bi-pencil me-2"></i>Editar Categoria de Serviço';
    document.getElementById('csId').value = c.id;
    document.getElementById('csCodigoDisplay').value = c.id;
    document.getElementById('csNome').value = c.nome || '';
    definirAtivo('csAtivo', c.ativo);
}
