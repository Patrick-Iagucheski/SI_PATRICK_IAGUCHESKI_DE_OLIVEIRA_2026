// Script da tela Marcas (Pages/Estoque/Marcas.cshtml).

// ===== Busca de Marcas (filtros na pagina -> atualiza a grid via AJAX) =====
let resultadosBusca = [];

['buscaId', 'buscaTermo'].forEach(id => {
    document.getElementById(id)?.addEventListener('keydown', e => {
        if (e.key === 'Enter') { e.preventDefault(); buscarMarcas(); }
    });
});

function montarLinha(m, i, token) {
    return '<tr>' +
        '<td>' + m.id + '</td>' +
        '<td><strong>' + (m.nome || '') + '</strong></td>' +
        '<td>' + statusBadge(m.ativo) + '</td>' +
        '<td class="text-center">' +
            '<button class="btn btn-icon btn-outline-primary" title="Editar" onclick="abrirEdicao(' + i + ')"><i class="bi bi-pencil"></i></button> ' +
            '<form method="post" action="?handler=Excluir" class="d-inline" onsubmit="return confirm(\'Deseja realmente excluir esta marca?\')">' +
                '<input type="hidden" name="__RequestVerificationToken" value="' + token + '">' +
                '<input type="hidden" name="id" value="' + m.id + '">' +
                '<button type="submit" class="btn btn-icon btn-outline-danger" title="Excluir"><i class="bi bi-trash"></i></button>' +
            '</form>' +
        '</td>' +
    '</tr>';
}

async function buscarMarcas() {
    const tbody = document.querySelector('#tblMarcas tbody');
    const id = document.getElementById('buscaId').value.trim();
    const termo = document.getElementById('buscaTermo').value.trim();
    const inativos = document.getElementById('buscaInativos').checked;

    const params = new URLSearchParams();
    if (id) params.append('id', id);
    if (termo) params.append('termo', termo);
    params.append('incluirInativos', inativos);

    tbody.innerHTML = '<tr><td colspan="4" class="text-center text-muted py-3">Buscando...</td></tr>';
    try {
        const resp = await fetch('?handler=BuscarMarcas&' + params.toString());
        resultadosBusca = await resp.json();

        const contador = document.getElementById('contadorRegistros');
        if (contador) contador.textContent = resultadosBusca.length + ' registros encontrados';

        if (!resultadosBusca.length) {
            tbody.innerHTML = '<tr><td colspan="4" class="text-center text-muted py-4">Nenhuma marca encontrada</td></tr>';
            return;
        }

        const token = document.querySelector('#formTokenAjax input[name="__RequestVerificationToken"]').value;
        tbody.innerHTML = resultadosBusca.map((m, i) => montarLinha(m, i, token)).join('');
    } catch (e) {
        tbody.innerHTML = '<tr><td colspan="4" class="text-center text-danger py-4">Erro ao buscar. Tente novamente.</td></tr>';
    }
}

function abrirEdicao(i) {
    const m = resultadosBusca[i];
    editarMarca(m.id, m.nome || '', (m.ativo ? 'True' : 'False'));
    bootstrap.Modal.getOrCreateInstance(document.getElementById('modalMarca')).show();
}

// ===== Modal (novo / editar) =====
function limparForm() {
    document.getElementById('modalTitle').innerHTML = '<i class="bi bi-tags me-2"></i>Nova Marca';
    document.getElementById('mId').value = '0';
    document.getElementById('mCodigoDisplay').value = '';
    document.getElementById('mNome').value = '';
    document.getElementById('mAtivo').value = 'true';
}

function editarMarca(id, nome, ativo) {
    document.getElementById('modalTitle').innerHTML = '<i class="bi bi-pencil me-2"></i>Editar Marca';
    document.getElementById('mId').value = id;
    document.getElementById('mCodigoDisplay').value = id;
    document.getElementById('mNome').value = nome;
    document.getElementById('mAtivo').value = ativo === 'True' ? 'true' : 'false';
}

