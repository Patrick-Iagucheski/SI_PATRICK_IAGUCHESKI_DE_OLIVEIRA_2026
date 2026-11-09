// Script da tela Unidades de Medida (Pages/Estoque/UnidadesMedida.cshtml).

// ===== Busca (filtros na pagina -> atualiza a grade via AJAX) =====
let resultadosBusca = [];

['buscaId', 'buscaTermo'].forEach(id => {
    document.getElementById(id)?.addEventListener('keydown', e => {
        if (e.key === 'Enter') { e.preventDefault(); buscarUnidades(); }
    });
});

function montarLinha(u, i) {
    return '<tr>' +
        '<td>' + u.id + '</td>' +
        '<td><strong>' + escaparHtml(u.sigla) + '</strong></td>' +
        '<td>' + escaparHtml(u.nome) + '</td>' +
        '<td>' + statusBadge(u.ativo) + '</td>' +
        '<td class="text-center">' +
            '<button class="btn btn-icon btn-outline-primary" title="Editar" onclick="abrirEdicao(' + i + ')"><i class="bi bi-pencil"></i></button> ' +
            '<form method="post" action="?handler=Excluir" class="d-inline" onsubmit="return confirm(\'Deseja realmente excluir esta unidade de medida?\')">' +
                campoToken() +
                '<input type="hidden" name="id" value="' + u.id + '">' +
                '<button type="submit" class="btn btn-icon btn-outline-danger" title="Excluir"><i class="bi bi-trash"></i></button>' +
            '</form>' +
        '</td>' +
    '</tr>';
}

async function buscarUnidades() {
    const tbody = document.querySelector('#tblUnidades tbody');
    const params = new URLSearchParams();
    const id = document.getElementById('buscaId').value.trim();
    const termo = document.getElementById('buscaTermo').value.trim();
    if (id) params.append('id', id);
    if (termo) params.append('termo', termo);
    params.append('incluirInativos', document.getElementById('buscaInativos').checked);

    tbody.innerHTML = '<tr><td colspan="5" class="text-center text-muted py-3">Buscando...</td></tr>';
    try {
        resultadosBusca = await (await fetch('?handler=BuscarUnidades&' + params.toString())).json();
        document.getElementById('contadorRegistros').textContent = resultadosBusca.length + ' registros encontrados';
        tbody.innerHTML = resultadosBusca.length
            ? resultadosBusca.map((u, i) => montarLinha(u, i)).join('')
            : '<tr><td colspan="5" class="text-center text-muted py-4">Nenhuma unidade de medida encontrada</td></tr>';
    } catch {
        tbody.innerHTML = '<tr><td colspan="5" class="text-center text-danger py-4">Erro ao buscar. Tente novamente.</td></tr>';
    }
}

function abrirEdicao(i) {
    mostrarRegistro('modalUnidade', resultadosBusca[i].registro);
    editarUnidade(resultadosBusca[i]);
    bootstrap.Modal.getOrCreateInstance(document.getElementById('modalUnidade')).show();
}

// ===== Modal (novo / editar) =====
function limparFormUnidade() {
    document.getElementById('umTitulo').innerHTML = '<i class="bi bi-rulers me-2"></i>Nova Unidade de Medida';
    document.getElementById('umId').value = '0';
    document.getElementById('umCodigoDisplay').value = '';
    document.getElementById('umSigla').value = '';
    document.getElementById('umNome').value = '';
    definirAtivo('umAtivo', true);
}

// u: registro da busca (UnidadesMedidaModel.Registro).
function editarUnidade(u) {
    document.getElementById('umTitulo').innerHTML = '<i class="bi bi-pencil me-2"></i>Editar Unidade de Medida';
    document.getElementById('umId').value = u.id;
    document.getElementById('umCodigoDisplay').value = u.id;
    document.getElementById('umSigla').value = u.sigla || '';
    document.getElementById('umNome').value = u.nome || '';
    definirAtivo('umAtivo', u.ativo);
}
