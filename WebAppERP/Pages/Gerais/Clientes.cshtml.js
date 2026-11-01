// Script da tela Clientes (Pages/Gerais/Clientes.cshtml).

// ===== Mascaras (funcoes em wwwroot/js/module.js; no banco salva so numeros) =====
aplicarMascara('formNrCpf', mascaraCPF);
aplicarMascara('formNrCnpj', mascaraCNPJ);
aplicarMascara('formNrCEP', mascaraCEP);
aplicarMascara('formNrTelefone', mascaraTelefone);
aplicarMascara('formNrRG', mascaraRG);

// ===== Tipo de Pessoa: alterna o documento exibido =====
function toggleTipoPessoa() {
    const tipo = document.getElementById('formTpPessoa').value;
    const wrapCpf = document.getElementById('wrapCpf');
    const wrapCnpj = document.getElementById('wrapCnpj');
    const wrapDoc = document.getElementById('wrapDoc');
    const lblNome = document.getElementById('lblNome');

    wrapCpf.classList.toggle('d-none', tipo !== 'F');
    wrapCnpj.classList.toggle('d-none', tipo !== 'J');
    wrapDoc.classList.toggle('d-none', tipo !== 'E');

    lblNome.textContent = tipo === 'J' ? 'Razão Social *' : 'Nome Completo *';
}

// ===== Validacao do formulario antes de enviar =====
document.querySelector('#modalCliente form')?.addEventListener('submit', function (e) {
    const tipo = document.getElementById('formTpPessoa').value;
    const cpf = document.getElementById('formNrCpf').value;
    const cnpj = document.getElementById('formNrCnpj').value;
    const email = document.getElementById('formDsEmail').value.trim();

    if (tipo === 'F' && cpf.replace(/\D/g, '') && !cpfValido(cpf)) {
        e.preventDefault();
        alert('CPF inválido. Verifique os números informados.');
        return;
    }
    if (tipo === 'J' && cnpj.replace(/\D/g, '') && !cnpjValido(cnpj)) {
        e.preventDefault();
        alert('CNPJ inválido. Verifique os números informados.');
        return;
    }
    if (email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
        e.preventDefault();
        alert('E-mail inválido. Verifique o formato (exemplo: nome@dominio.com).');
        return;
    }
});

function limparForm() {
    document.getElementById('modalTitle').innerHTML = '<i class="bi bi-person-plus me-2"></i>Novo Cliente';
    ['formNmCliente','formNrCpf','formNrCnpj','formNrDocumento','formNrRG','formNrTelefone','formDsEmail','formDsEndereco','formNrNumero','formDsComplemento','formDsBairro','formNrCEP','formDtNascimento','formDsSexo','formDsObservacao','formCodigoDisplay'].forEach(id => {
        document.getElementById(id).value = '';
    });
    document.getElementById('formIdCliente').value = '0';
    document.getElementById('formTpPessoa').value = 'F';
    document.getElementById('formFlAtivo').value = 'true';
    limparCidade('formIdCidade');
    toggleTipoPessoa();
}

function editarCliente(id, nome, tipo, cpf, cnpj, doc, rg, tel, email, end, numero, complemento, bairro, cidade, cep, nasc, sexo, obs, ativo) {
    document.getElementById('modalTitle').innerHTML = '<i class="bi bi-pencil me-2"></i>Editar Cliente';
    document.getElementById('formIdCliente').value = id;
    document.getElementById('formCodigoDisplay').value = id;
    document.getElementById('formNmCliente').value = nome;
    document.getElementById('formTpPessoa').value = tipo || 'F';
    document.getElementById('formNrCpf').value = mascaraCPF(cpf || '');
    document.getElementById('formNrCnpj').value = mascaraCNPJ(cnpj || '');
    document.getElementById('formNrDocumento').value = doc || '';
    document.getElementById('formNrRG').value = mascaraRG(rg || '');
    document.getElementById('formNrTelefone').value = mascaraTelefone(tel || '');
    document.getElementById('formDsEmail').value = email || '';
    document.getElementById('formDsEndereco').value = end || '';
    document.getElementById('formNrNumero').value = numero || '';
    document.getElementById('formDsComplemento').value = complemento || '';
    document.getElementById('formDsBairro').value = bairro || '';
    definirCidade('formIdCidade', cidade);
    document.getElementById('formNrCEP').value = mascaraCEP(cep || '');
    document.getElementById('formDtNascimento').value = nasc || '';
    document.getElementById('formDsSexo').value = sexo || '';
    document.getElementById('formDsObservacao').value = obs || '';
    document.getElementById('formFlAtivo').value = ativo === 'True' ? 'true' : 'false';
    toggleTipoPessoa();
}

// ===== Busca de Clientes (filtros na pagina -> atualiza a grid via AJAX) =====
let resultadosBusca = [];

aplicarMascara('buscaDocumento', mascaraCPF);
['buscaId', 'buscaNome', 'buscaDocumento', 'buscaEmail'].forEach(id => {
    document.getElementById(id)?.addEventListener('keydown', e => {
        if (e.key === 'Enter') { e.preventDefault(); buscarClientes(); }
    });
});

function tipoLabel(t) {
    if (t === 'J') return 'Juridica';
    if (t === 'E') return 'Estrangeiro';
    return 'Fisica';
}

function documentoExibicao(f) {
    if (f.tipoPessoa === 'J') return f.cnpj ? mascaraCNPJ(f.cnpj) : '-';
    if (f.tipoPessoa === 'E') return f.documento || '-';
    return f.cpf ? mascaraCPF(f.cpf) : '-';
}

function montarLinha(f, i, token) {
    return '<tr>' +
        '<td>' + f.id + '</td>' +
        '<td><strong>' + (f.nome || '') + '</strong></td>' +
        '<td>' + tipoLabel(f.tipoPessoa) + '</td>' +
        '<td class="text-nowrap">' + documentoExibicao(f) + '</td>' +
        '<td class="text-nowrap">' + (f.tel ? mascaraTelefone(f.tel) : '-') + '</td>' +
        '<td>' + (f.email || '-') + '</td>' +
        '<td>' + (f.cidadeNome || '-') + '</td>' +
        '<td>' + statusBadge(f.ativo) + '</td>' +
        '<td class="text-center">' +
            '<button class="btn btn-icon btn-outline-primary" title="Editar" onclick="abrirEdicao(' + i + ')"><i class="bi bi-pencil"></i></button> ' +
            '<form method="post" action="?handler=Excluir" class="d-inline" onsubmit="return confirm(\'Deseja realmente excluir este cliente?\')">' +
                '<input type="hidden" name="__RequestVerificationToken" value="' + token + '">' +
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

        const token = document.querySelector('#formTokenAjax input[name="__RequestVerificationToken"]').value;
        tbody.innerHTML = resultadosBusca.map((f, i) => montarLinha(f, i, token)).join('');
    } catch (e) {
        tbody.innerHTML = '<tr><td colspan="9" class="text-center text-danger py-4">Erro ao buscar. Tente novamente.</td></tr>';
    }
}

function abrirEdicao(i) {
    const f = resultadosBusca[i];
    editarCliente(
        f.id, f.nome || '', f.tipoPessoa || 'F', f.cpf || '', f.cnpj || '', f.documento || '',
        f.rg || '', f.tel || '', f.email || '', f.end || '', f.numero || '', f.complemento || '',
        f.bairro || '', (f.idCidade ?? ''), f.cep || '', f.nasc || '', f.sexo || '',
        f.obs || '', (f.ativo ? 'True' : 'False')
    );
    bootstrap.Modal.getOrCreateInstance(document.getElementById('modalCliente')).show();
}

