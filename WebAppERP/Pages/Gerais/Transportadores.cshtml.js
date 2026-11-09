// Script da tela Transportadores (Pages/Gerais/Transportadores.cshtml).
// Os valores que vem do servidor estao no bloco JSON #dadosTela da pagina.
// Modal de cadastro: componente CadastroTransportador (limpar, editar e pesquisa).
const DADOS_TELA = JSON.parse(document.getElementById('dadosTela').textContent);

// Transportadoras ja carregadas na pagina (para edicao das linhas iniciais)
const dadosIniciais = DADOS_TELA.dadosIniciais;

let resultadosBusca = [];

function editarPorId(id) {
    const t = dadosIniciais.find(x => x.id === id);
    if (t) editarTransportador(t);
}

function abrirEdicao(i) {
    mostrarRegistro('modalTransportador', resultadosBusca[i].registro);
    editarTransportador(resultadosBusca[i]);
    bootstrap.Modal.getOrCreateInstance(document.getElementById('modalTransportador')).show();
}

// ===== Busca de transportadoras via AJAX =====
aplicarMascara('buscaDocumento', mascaraCpfCnpj);
['buscaId', 'buscaRazao', 'buscaFantasia', 'buscaDocumento'].forEach(id => {
    document.getElementById(id)?.addEventListener('keydown', e => {
        if (e.key === 'Enter') { e.preventDefault(); buscarTransportadores(); }
    });
});

function montarLinha(t, i) {
    const tel = t.celular || t.telefoneFixo;
    return '<tr>' +
        '<td>' + t.id + '</td>' +
        '<td><strong>' + escaparHtml(t.razaoSocial || '') + '</strong></td>' +
        '<td>' + escaparHtml(t.nomeFantasia || '-') + '</td>' +
        '<td>' + documentoTransportador(t) + '</td>' +
        '<td>' + (tel ? mascaraTelefone(tel) : '-') + '</td>' +
        '<td>' + escaparHtml(t.cidadeNome || '-') + '</td>' +
        '<td>' + statusBadge(t.ativo) + '</td>' +
        '<td class="text-center">' +
            '<button class="btn btn-icon btn-outline-primary" title="Editar" onclick="abrirEdicao(' + i + ')"><i class="bi bi-pencil"></i></button> ' +
            '<form method="post" action="?handler=Excluir" class="d-inline" onsubmit="return confirm(\'Deseja realmente excluir esta transportadora?\')">' +
                campoToken() +
                '<input type="hidden" name="id" value="' + t.id + '">' +
                '<button type="submit" class="btn btn-icon btn-outline-danger" title="Excluir"><i class="bi bi-trash"></i></button>' +
            '</form>' +
        '</td>' +
    '</tr>';
}

async function buscarTransportadores() {
    const tbody = document.querySelector('#tblTransportadores tbody');
    const params = new URLSearchParams();
    [['id', 'buscaId'], ['razao', 'buscaRazao'], ['fantasia', 'buscaFantasia'], ['documento', 'buscaDocumento']].forEach(([param, id]) => {
        const v = document.getElementById(id).value.trim();
        if (v) params.append(param, v);
    });
    params.append('incluirInativos', document.getElementById('buscaInativos').checked);

    tbody.innerHTML = '<tr><td colspan="8" class="text-center text-muted py-3">Buscando...</td></tr>';
    try {
        const resp = await fetch('?handler=BuscarTransportadores&' + params.toString());
        resultadosBusca = await resp.json();

        document.getElementById('contadorRegistros').textContent = resultadosBusca.length + ' registros encontrados';

        tbody.innerHTML = resultadosBusca.length
            ? resultadosBusca.map((t, i) => montarLinha(t, i)).join('')
            : '<tr><td colspan="8" class="text-center text-muted py-4">Nenhuma transportadora encontrada</td></tr>';
    } catch (e) {
        tbody.innerHTML = '<tr><td colspan="8" class="text-center text-danger py-4">Erro ao buscar. Tente novamente.</td></tr>';
    }
}
