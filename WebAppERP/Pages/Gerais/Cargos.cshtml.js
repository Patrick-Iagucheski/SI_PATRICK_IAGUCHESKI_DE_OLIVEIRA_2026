// Script da tela Cargos (Pages/Gerais/Cargos.cshtml).

// ===== Busca de Cargos (filtros na pagina -> atualiza a grid via AJAX) =====
let resultadosBusca = [];

['buscaId', 'buscaTermo'].forEach(id => {
    document.getElementById(id)?.addEventListener('keydown', e => {
        if (e.key === 'Enter') { e.preventDefault(); buscarCargos(); }
    });
});

function statusBadge(ativo) {
    return ativo
        ? '<span class="badge-status badge-ativo">Ativo</span>'
        : '<span class="badge-status badge-inativo">Inativo</span>';
}

function montarLinha(c, i, token) {
    const comissao = (c.comissao !== null && c.comissao !== undefined)
        ? (parseFloat(c.comissao).toString().replace('.', ',') + '%') : '-';
    const desc = (c.desc && c.desc.trim()) ? c.desc : '-';
    return '<tr>' +
        '<td>' + c.id + '</td>' +
        '<td><strong>' + (c.nome || '') + '</strong></td>' +
        '<td>' + desc + '</td>' +
        '<td>' + comissao + '</td>' +
        '<td>' + statusBadge(c.ativo) + '</td>' +
        '<td class="text-center">' +
            '<button class="btn btn-icon btn-outline-primary" title="Editar" onclick="abrirEdicao(' + i + ')"><i class="bi bi-pencil"></i></button> ' +
            '<form method="post" action="?handler=Excluir" class="d-inline" onsubmit="return confirm(\'Deseja realmente excluir este cargo?\')">' +
                '<input type="hidden" name="__RequestVerificationToken" value="' + token + '">' +
                '<input type="hidden" name="id" value="' + c.id + '">' +
                '<button type="submit" class="btn btn-icon btn-outline-danger" title="Excluir"><i class="bi bi-trash"></i></button>' +
            '</form>' +
        '</td>' +
    '</tr>';
}

async function buscarCargos() {
    const tbody = document.querySelector('#tblCargos tbody');
    const id = document.getElementById('buscaId').value.trim();
    const termo = document.getElementById('buscaTermo').value.trim();
    const inativos = document.getElementById('buscaInativos').checked;

    const params = new URLSearchParams();
    if (id) params.append('id', id);
    if (termo) params.append('termo', termo);
    params.append('incluirInativos', inativos);

    tbody.innerHTML = '<tr><td colspan="6" class="text-center text-muted py-3">Buscando...</td></tr>';
    try {
        const resp = await fetch('?handler=BuscarCargos&' + params.toString());
        resultadosBusca = await resp.json();

        const contador = document.getElementById('contadorRegistros');
        if (contador) contador.textContent = resultadosBusca.length + ' registros encontrados';

        if (!resultadosBusca.length) {
            tbody.innerHTML = '<tr><td colspan="6" class="text-center text-muted py-4">Nenhum cargo encontrado</td></tr>';
            return;
        }

        const token = document.querySelector('#formTokenAjax input[name="__RequestVerificationToken"]').value;
        tbody.innerHTML = resultadosBusca.map((c, i) => montarLinha(c, i, token)).join('');
    } catch (e) {
        tbody.innerHTML = '<tr><td colspan="6" class="text-center text-danger py-4">Erro ao buscar. Tente novamente.</td></tr>';
    }
}

function abrirEdicao(i) {
    const c = resultadosBusca[i];
    editarCargo(
        c.id, c.nome || '', c.desc || '',
        (c.comissao ?? ''), (c.ativo ? 'True' : 'False')
    );
    bootstrap.Modal.getOrCreateInstance(document.getElementById('modalCargo')).show();
}

// ===== Modal (novo / editar) =====
function limparForm() {
    document.getElementById('modalTitle').innerHTML = '<i class="bi bi-briefcase me-2"></i>Novo Cargo';
    document.getElementById('cId').value = '0';
    document.getElementById('cNome').value = '';
    document.getElementById('cDesc').value = '';
    document.getElementById('cComissao').value = '';
    document.getElementById('cAtivo').value = 'true';
}

function editarCargo(id, nome, desc, comissao, ativo) {
    document.getElementById('modalTitle').innerHTML = '<i class="bi bi-pencil me-2"></i>Editar Cargo';
    document.getElementById('cId').value = id;
    document.getElementById('cNome').value = nome;
    document.getElementById('cDesc').value = desc;
    document.getElementById('cComissao').value = comissao;
    document.getElementById('cAtivo').value = ativo === 'True' ? 'true' : 'false';
}

