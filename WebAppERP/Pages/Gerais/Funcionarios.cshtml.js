// Script da tela Funcionarios (Pages/Gerais/Funcionarios.cshtml).
// Modal de cadastro: componente CadastroFuncionario (mascaras, limpar, editar e cargo).

// ===== Busca de Funcionario (filtros na pagina -> atualiza a grid principal via AJAX) =====
let resultadosBusca = [];

// Mascara de CPF no filtro + Enter dispara a busca
aplicarMascara('buscaCpf', mascaraCPF);
['buscaId', 'buscaNome', 'buscaApelido', 'buscaCpf'].forEach(id => {
    document.getElementById(id)?.addEventListener('keydown', e => {
        if (e.key === 'Enter') { e.preventDefault(); buscarFuncionarios(); }
    });
});

function montarLinha(f, i) {
    const adm = dataBr(f.adm, '');
    const com = (f.com !== null && f.com !== undefined) ? f.com + '%' : '-';
    return '<tr>' +
        '<td>' + f.id + '</td>' +
        '<td><strong>' + escaparHtml(f.nome || '') + '</strong></td>' +
        '<td>' + mascaraCPF(f.cpf || '') + '</td>' +
        '<td>' + escaparHtml(f.cargoNome || '-') + '</td>' +
        '<td>' + (f.tel ? mascaraTelefone(f.tel) : '-') + '</td>' +
        '<td>' + adm + '</td>' +
        '<td>' + com + '</td>' +
        '<td>' + statusBadge(f.ativo) + '</td>' +
        '<td class="text-center">' +
            '<button class="btn btn-icon btn-outline-primary" title="Editar" onclick="abrirEdicao(' + i + ')"><i class="bi bi-pencil"></i></button> ' +
            '<form method="post" action="?handler=Excluir" class="d-inline" onsubmit="return confirm(\'Deseja realmente excluir este funcionário?\')">' +
                campoToken() +
                '<input type="hidden" name="id" value="' + f.id + '">' +
                '<button type="submit" class="btn btn-icon btn-outline-danger" title="Excluir"><i class="bi bi-trash"></i></button>' +
            '</form>' +
        '</td>' +
    '</tr>';
}

async function buscarFuncionarios() {
    const tbody = document.querySelector('#tblFuncionarios tbody');
    const id = document.getElementById('buscaId').value.trim();
    const nome = document.getElementById('buscaNome').value.trim();
    const apelido = document.getElementById('buscaApelido').value.trim();
    const cpf = document.getElementById('buscaCpf').value.replace(/\D/g, '');
    const inativos = document.getElementById('buscaInativos').checked;

    const params = new URLSearchParams();
    if (id) params.append('id', id);
    if (nome) params.append('nome', nome);
    if (apelido) params.append('apelido', apelido);
    if (cpf) params.append('cpf', cpf);
    params.append('incluirInativos', inativos);

    tbody.innerHTML = '<tr><td colspan="9" class="text-center text-muted py-3">Buscando...</td></tr>';
    try {
        const resp = await fetch('?handler=BuscarFuncionarios&' + params.toString());
        resultadosBusca = await resp.json();

        const contador = document.getElementById('contadorRegistros');
        if (contador) contador.textContent = resultadosBusca.length + ' registros encontrados';

        if (!resultadosBusca.length) {
            tbody.innerHTML = '<tr><td colspan="9" class="text-center text-muted py-4">Nenhum funcionário encontrado</td></tr>';
            return;
        }
        tbody.innerHTML = resultadosBusca.map((f, i) => montarLinha(f, i)).join('');
    } catch (e) {
        tbody.innerHTML = '<tr><td colspan="9" class="text-center text-danger py-4">Erro ao buscar. Tente novamente.</td></tr>';
    }
}

function abrirEdicao(i) {
    editarFuncionarioPorRegistro(resultadosBusca[i]);
    bootstrap.Modal.getOrCreateInstance(document.getElementById('modalFuncionario')).show();
}

