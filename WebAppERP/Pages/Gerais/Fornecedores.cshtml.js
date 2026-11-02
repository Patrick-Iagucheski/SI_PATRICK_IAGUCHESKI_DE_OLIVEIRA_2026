// Script da tela Fornecedores (Pages/Gerais/Fornecedores.cshtml).
// Os valores que vem do servidor estao no bloco JSON #dadosTela da pagina.
const DADOS_TELA = JSON.parse(document.getElementById('dadosTela').textContent);

// Dados dos fornecedores ja carregados na pagina (para edicao das linhas iniciais)
const dadosIniciais = DADOS_TELA.dadosIniciais;

let resultadosBusca = [];

function editarPorId(id) {
    const f = dadosIniciais.find(x => x.id === id);
    if (f) editarFornecedor(f);
}

function abrirEdicao(i) {
    editarFornecedor(resultadosBusca[i]);
    bootstrap.Modal.getOrCreateInstance(document.getElementById('modalFornecedor')).show();
}

// ===== Busca de Fornecedores via AJAX =====
aplicarMascara('buscaDocumento', mascaraCNPJ);
['buscaId', 'buscaRazao', 'buscaFantasia', 'buscaDocumento'].forEach(id => {
    document.getElementById(id)?.addEventListener('keydown', e => {
        if (e.key === 'Enter') { e.preventDefault(); buscarFornecedores(); }
    });
});

function documentoExibicao(f) {
    if (f.tipoPessoa === 'F') return f.cpf ? mascaraCPF(f.cpf) : '-';
    if (f.tipoPessoa === 'E' || f.tipoPessoa === 'O') return f.documento || '-';
    return f.cnpj ? mascaraCNPJ(f.cnpj) : '-';
}

function montarLinha(f, i) {
    const tel = f.celular || f.telefoneFixo;
    return '<tr>' +
        '<td>' + f.id + '</td>' +
        '<td><strong>' + (f.razaoSocial || '') + '</strong></td>' +
        '<td>' + (f.nomeFantasia || '-') + '</td>' +
        '<td>' + documentoExibicao(f) + '</td>' +
        '<td>' + (tel ? mascaraTelefone(tel) : '-') + '</td>' +
        '<td>' + (f.cidadeNome || '-') + '</td>' +
        '<td>' + statusBadge(f.ativo) + '</td>' +
        '<td class="text-center">' +
            '<button class="btn btn-icon btn-outline-primary" title="Editar" onclick="abrirEdicao(' + i + ')"><i class="bi bi-pencil"></i></button> ' +
            '<form method="post" action="?handler=Excluir" class="d-inline" onsubmit="return confirm(\'Deseja realmente excluir este fornecedor?\')">' +
                campoToken() +
                '<input type="hidden" name="id" value="' + f.id + '">' +
                '<button type="submit" class="btn btn-icon btn-outline-danger" title="Excluir"><i class="bi bi-trash"></i></button>' +
            '</form>' +
        '</td>' +
    '</tr>';
}

async function buscarFornecedores() {
    const tbody = document.querySelector('#tblFornecedores tbody');
    const id = document.getElementById('buscaId').value.trim();
    const razao = document.getElementById('buscaRazao').value.trim();
    const fantasia = document.getElementById('buscaFantasia').value.trim();
    const documento = document.getElementById('buscaDocumento').value.trim();
    const inativos = document.getElementById('buscaInativos').checked;

    const params = new URLSearchParams();
    if (id) params.append('id', id);
    if (razao) params.append('razao', razao);
    if (fantasia) params.append('fantasia', fantasia);
    if (documento) params.append('documento', documento);
    params.append('incluirInativos', inativos);

    tbody.innerHTML = '<tr><td colspan="8" class="text-center text-muted py-3">Buscando...</td></tr>';
    try {
        const resp = await fetch('?handler=BuscarFornecedores&' + params.toString());
        resultadosBusca = await resp.json();

        const contador = document.getElementById('contadorRegistros');
        if (contador) contador.textContent = resultadosBusca.length + ' registros encontrados';

        if (!resultadosBusca.length) {
            tbody.innerHTML = '<tr><td colspan="8" class="text-center text-muted py-4">Nenhum fornecedor encontrado</td></tr>';
            return;
        }
        tbody.innerHTML = resultadosBusca.map((f, i) => montarLinha(f, i)).join('');
    } catch (e) {
        tbody.innerHTML = '<tr><td colspan="8" class="text-center text-danger py-4">Erro ao buscar. Tente novamente.</td></tr>';
    }
}

