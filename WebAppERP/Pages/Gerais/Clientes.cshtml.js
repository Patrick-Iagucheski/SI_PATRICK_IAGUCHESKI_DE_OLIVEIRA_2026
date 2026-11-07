// Script da tela Clientes (Pages/Gerais/Clientes.cshtml).
// Modal de cadastro: componente CadastroCliente (mascaras, validacao, limpar e editar).

// ===== Busca de Clientes (filtros na pagina -> atualiza a grid via AJAX) =====
let resultadosBusca = [];

// Documento sem mascara: o filtro aceita CPF, CNPJ ou documento estrangeiro.
['buscaId', 'buscaNome', 'buscaDocumento', 'buscaEmail'].forEach(id => {
    document.getElementById(id)?.addEventListener('keydown', e => {
        if (e.key === 'Enter') { e.preventDefault(); buscarClientes(); }
    });
});

function tipoLabel(t) {
    if (t === 'J') return 'Jurídica';
    if (t === 'E') return 'Estrangeiro';
    return 'Física';
}

function montarLinha(f, i) {
    return '<tr>' +
        '<td>' + f.id + '</td>' +
        '<td><strong>' + escaparHtml(f.nome || '') + '</strong></td>' +
        '<td>' + tipoLabel(f.tipoPessoa) + '</td>' +
        '<td class="text-nowrap">' + documentoCliente(f) + '</td>' +
        '<td class="text-nowrap">' + telefoneCliente(f) + '</td>' +
        '<td>' + escaparHtml(f.email || '-') + '</td>' +
        '<td>' + escaparHtml(f.cidadeNome || '-') + '</td>' +
        '<td>' + statusBadge(f.ativo) + '</td>' +
        '<td class="text-center">' +
            '<button class="btn btn-icon btn-outline-primary" title="Editar" onclick="abrirEdicao(' + i + ')"><i class="bi bi-pencil"></i></button> ' +
            '<form method="post" action="?handler=Excluir" class="d-inline" onsubmit="return confirm(\'Deseja realmente excluir este cliente?\')">' +
                campoToken() +
                '<input type="hidden" name="id" value="' + f.id + '">' +
                '<button type="submit" class="btn btn-icon btn-outline-danger" title="Excluir"><i class="bi bi-trash"></i></button>' +
            '</form>' +
        '</td>' +
    '</tr>';
}

async function buscarClientes() {
    const tbody = document.querySelector('#tblClientes tbody');
    const id = document.getElementById('buscaId').value.trim();
    const nome = document.getElementById('buscaNome').value.trim();
    const documento = document.getElementById('buscaDocumento').value.trim();
    const email = document.getElementById('buscaEmail').value.trim();
    const inativos = document.getElementById('buscaInativos').checked;

    const params = new URLSearchParams();
    if (id) params.append('id', id);
    if (nome) params.append('nome', nome);
    if (documento) params.append('documento', documento);
    if (email) params.append('email', email);
    params.append('incluirInativos', inativos);

    tbody.innerHTML = '<tr><td colspan="9" class="text-center text-muted py-3">Buscando...</td></tr>';
    try {
        const resp = await fetch('?handler=BuscarClientes&' + params.toString());
        resultadosBusca = await resp.json();

        const contador = document.getElementById('contadorRegistros');
        if (contador) contador.textContent = resultadosBusca.length + ' registros encontrados';

        if (!resultadosBusca.length) {
            tbody.innerHTML = '<tr><td colspan="9" class="text-center text-muted py-4">Nenhum cliente encontrado</td></tr>';
            return;
        }
        tbody.innerHTML = resultadosBusca.map((f, i) => montarLinha(f, i)).join('');
    } catch (e) {
        tbody.innerHTML = '<tr><td colspan="9" class="text-center text-danger py-4">Erro ao buscar. Tente novamente.</td></tr>';
    }
}

function abrirEdicao(i) {
    mostrarRegistro('modalCliente', resultadosBusca[i].registro);
    editarCliente(resultadosBusca[i]);
    bootstrap.Modal.getOrCreateInstance(document.getElementById('modalCliente')).show();
}

