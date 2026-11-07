// Script do componente de cadastro de cliente
// (Pages/Shared/Components/CadastroCliente/Default.cshtml).
// Usado pela tela de Clientes e pelas pesquisas de cliente (Venda, Agendamentos).

// ===== Mascaras (funcoes em wwwroot/js/module.js; no banco salva so numeros) =====
aplicarMascara('cliNrCpf', mascaraCPF);
aplicarMascara('cliNrCnpj', mascaraCNPJ);
aplicarMascara('cliNrCEP', mascaraCEP);
aplicarMascara('cliNrTelefone', mascaraTelefone);
aplicarMascara('cliNrRG', mascaraRG);

// ===== Tipo de Pessoa: alterna o documento exibido =====
function toggleTipoPessoaCliente() {
    const tipo = document.getElementById('cliTpPessoa').value;
    const wrapCpf = document.getElementById('cliWrapCpf');
    const wrapCnpj = document.getElementById('cliWrapCnpj');
    const wrapDoc = document.getElementById('cliWrapDoc');
    const lblNome = document.getElementById('cliLblNome');

    wrapCpf.classList.toggle('d-none', tipo !== 'F');
    wrapCnpj.classList.toggle('d-none', tipo !== 'J');
    wrapDoc.classList.toggle('d-none', tipo !== 'E');

    // Obrigatorio so o documento visivel (CPF na fisica, CNPJ na juridica):
    // campo escondido com required travaria o envio do formulario.
    document.getElementById('cliNrCpf').required = tipo === 'F';
    document.getElementById('cliNrCnpj').required = tipo === 'J';

    lblNome.textContent = tipo === 'J' ? 'Razão Social *' : 'Nome Completo *';
}

// ===== Validacao do formulario antes de enviar =====
document.querySelector('#modalCliente form')?.addEventListener('submit', function (e) {
    const tipo = document.getElementById('cliTpPessoa').value;
    const cpf = document.getElementById('cliNrCpf').value;
    const cnpj = document.getElementById('cliNrCnpj').value;
    const email = document.getElementById('cliDsEmail').value.trim();

    if (tipo === 'F' && cpf.replace(/\D/g, '') && !cpfValido(cpf)) {
        e.preventDefault();
        exibirErro(this, 'CPF inválido. Verifique os números informados.');
        return;
    }
    if (tipo === 'J' && cnpj.replace(/\D/g, '') && !cnpjValido(cnpj)) {
        e.preventDefault();
        exibirErro(this, 'CNPJ inválido. Verifique os números informados.');
        return;
    }
    if (email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
        e.preventDefault();
        exibirErro(this, 'E-mail inválido. Verifique o formato (exemplo: nome@dominio.com).');
        return;
    }
});

function limparFormCliente() {
    document.getElementById('cliTitulo').innerHTML = '<i class="bi bi-person-plus me-2"></i>Novo Cliente';
    ['cliNmCliente','cliNrCpf','cliNrCnpj','cliNrDocumento','cliNrRG','cliNrTelefone','cliDsEmail','cliDsEndereco','cliNrNumero','cliDsComplemento','cliDsBairro','cliNrCEP','cliDtNascimento','cliDsSexo','cliDsObservacao','cliCodigoDisplay'].forEach(id => {
        document.getElementById(id).value = '';
    });
    document.getElementById('cliIdCliente').value = '0';
    document.getElementById('cliTpPessoa').value = 'F';
    document.getElementById('cliFlAtivo').value = 'true';
    limparCidade('cliIdCidade');
    toggleTipoPessoaCliente();
}

function editarCliente(id, nome, tipo, cpf, cnpj, doc, rg, tel, email, end, numero, complemento, bairro, cidade, cep, nasc, sexo, obs, ativo) {
    document.getElementById('cliTitulo').innerHTML = '<i class="bi bi-pencil me-2"></i>Editar Cliente';
    document.getElementById('cliIdCliente').value = id;
    document.getElementById('cliCodigoDisplay').value = id;
    document.getElementById('cliNmCliente').value = nome;
    document.getElementById('cliTpPessoa').value = tipo || 'F';
    document.getElementById('cliNrCpf').value = mascaraCPF(cpf || '');
    document.getElementById('cliNrCnpj').value = mascaraCNPJ(cnpj || '');
    document.getElementById('cliNrDocumento').value = doc || '';
    document.getElementById('cliNrRG').value = mascaraRG(rg || '');
    document.getElementById('cliNrTelefone').value = mascaraTelefone(tel || '');
    document.getElementById('cliDsEmail').value = email || '';
    document.getElementById('cliDsEndereco').value = end || '';
    document.getElementById('cliNrNumero').value = numero || '';
    document.getElementById('cliDsComplemento').value = complemento || '';
    document.getElementById('cliDsBairro').value = bairro || '';
    definirCidade('cliIdCidade', cidade);
    document.getElementById('cliNrCEP').value = mascaraCEP(cep || '');
    document.getElementById('cliDtNascimento').value = nasc || '';
    document.getElementById('cliDsSexo').value = sexo || '';
    document.getElementById('cliDsObservacao').value = obs || '';
    document.getElementById('cliFlAtivo').value = ativo === 'True' ? 'true' : 'false';
    toggleTipoPessoaCliente();
}

function urlClientes(handler) {
    return document.getElementById('modalCliente').dataset.urlCadastro + '?handler=' + handler;
}

// Registro da pesquisa (BuscarClientes) no formulario de edicao.
function editarClientePorRegistro(f) {
    editarCliente(
        f.id, f.nome || '', f.tipoPessoa || 'F', f.cpf || '', f.cnpj || '', f.documento || '',
        f.rg || '', f.tel || '', f.email || '', f.end || '', f.numero || '', f.complemento || '',
        f.bairro || '', (f.idCidade ?? ''), f.cep || '', f.nasc || '', f.sexo || '',
        f.obs || '', (f.ativo ? 'True' : 'False')
    );
}

// ===== Pesquisa de cliente (a lupa do campo de cliente de outras telas) =====
// opcoes: o que a tela faz com o registro (ver abrirPesquisaComCadastro no module.js).
function documentoCliente(c) {
    if (c.tipoPessoa === 'J') return c.cnpj ? mascaraCNPJ(c.cnpj) : '-';
    if (c.tipoPessoa === 'E') return escaparHtml(c.documento || '-');
    return c.cpf ? mascaraCPF(c.cpf) : '-';
}

const PESQUISA_CLIENTE = {
    chave: 'cliente',
    titulo: 'Pesquisar Cliente',
    // Mesma busca da tela de Clientes (so ativos, sem diferenciar acento).
    url: () => urlClientes('BuscarClientes'),
    filtros: [
        { param: 'id', rotulo: 'ID', tipo: 'number', col: 2 },
        { param: 'nome', rotulo: 'Nome', col: 4 },
        { param: 'documento', rotulo: 'CPF / CNPJ / Documento', col: 3 },
        { param: 'email', rotulo: 'E-mail', col: 3 }
    ],
    colunas: ['Código', 'Nome', 'CPF / CNPJ', 'Telefone', 'Cidade'],
    celulas: c => ['<strong>' + c.id + '</strong>', escaparHtml(c.nome), documentoCliente(c),
                   c.tel ? mascaraTelefone(c.tel) : '-', escaparHtml(c.cidadeNome || '-')],
    modal: 'modalCliente',
    evento: 'cliente:salvo',
    preencher: editarClientePorRegistro,
    limpar: limparFormCliente,
    urlExcluir: () => urlClientes('Excluir'),
    nome: c => c.nome
};

function abrirPesquisaCliente(opcoes) {
    abrirPesquisaComCadastro(PESQUISA_CLIENTE, opcoes);
}
