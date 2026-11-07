// Script do componente de cadastro de cliente
// (Pages/Shared/Components/CadastroCliente/Default.cshtml).
// Usado pela tela de Clientes e pelas pesquisas de cliente (Venda, Agendamentos).

// ===== Mascaras (funcoes em wwwroot/js/module.js; no banco salva so numeros) =====
aplicarMascara('cliNrCpf', mascaraCPF);
aplicarMascara('cliNrCnpj', mascaraCNPJ);
aplicarMascara('cliNrCEP', mascaraCEP);
aplicarMascara('cliNrTelefoneFixo', mascaraTelefone);
aplicarMascara('cliNrCelular', mascaraTelefone);
aplicarMascara('cliNrRG', mascaraRG);
// Inscricao estadual: so digitos.
document.getElementById('cliNrInscEstadual')?.addEventListener('input', e => { e.target.value = e.target.value.replace(/\D/g, ''); });

// ===== Tipo de Pessoa: alterna o documento e os campos de pessoa =====
// RG so na fisica; sexo e nascimento na fisica e no estrangeiro (nao na
// juridica); nome fantasia e inscricoes so na juridica. Campo escondido e
// limpo, para nao gravar valor de outro tipo.
function toggleTipoPessoaCliente() {
    const tipo = document.getElementById('cliTpPessoa').value;
    const lblNome = document.getElementById('cliLblNome');
    const mostrar = (wrap, campo, visivel) => {
        document.getElementById(wrap).classList.toggle('d-none', !visivel);
        if (!visivel) document.getElementById(campo).value = '';
    };

    mostrar('cliWrapCpf', 'cliNrCpf', tipo === 'F');
    mostrar('cliWrapCnpj', 'cliNrCnpj', tipo === 'J');
    mostrar('cliWrapDoc', 'cliNrDocumento', tipo === 'E');
    mostrar('cliWrapRG', 'cliNrRG', tipo === 'F');
    mostrar('cliWrapSexo', 'cliDsSexo', tipo !== 'J');
    mostrar('cliWrapNascimento', 'cliDtNascimento', tipo !== 'J');
    mostrar('cliWrapFantasia', 'cliDsNomeFantasia', tipo === 'J');
    document.getElementById('cliQuebraNome').classList.toggle('d-none', tipo !== 'J');
    mostrar('cliWrapInscEstadual', 'cliNrInscEstadual', tipo === 'J');
    mostrar('cliWrapInscMunicipal', 'cliNrInscMunicipal', tipo === 'J');

    // Linha dos documentos: 4 campos na fisica (3 colunas cada) e 3 na
    // juridica e no estrangeiro (4 colunas cada).
    const largura = tipo === 'F' ? 'col-md-3' : 'col-md-4';
    ['cliWrapCpf', 'cliWrapCnpj', 'cliWrapDoc', 'cliWrapInscEstadual', 'cliWrapInscMunicipal',
     'cliWrapRG', 'cliWrapSexo', 'cliWrapNascimento'].forEach(id => {
        const wrap = document.getElementById(id);
        const oculto = wrap.classList.contains('d-none');
        wrap.className = largura + (oculto ? ' d-none' : '');
    });

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
    if (tipo === 'J') {
        const ie = document.getElementById('cliNrInscEstadual').value.replace(/\D/g, '');
        if (ie && (ie.length < 8 || ie.length > 13)) { e.preventDefault(); exibirErro(this, 'Inscrição Estadual deve ter de 8 a 13 dígitos.'); return; }
        const im = document.getElementById('cliNrInscMunicipal').value.trim();
        if (im && (im.length < 7 || im.length > 15)) { e.preventDefault(); exibirErro(this, 'Inscrição Municipal deve ter de 7 a 15 caracteres.'); return; }
    }
    if (email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
        e.preventDefault();
        exibirErro(this, 'E-mail inválido. Verifique o formato (exemplo: nome@dominio.com).');
        return;
    }
});

function limparFormCliente() {
    document.getElementById('cliTitulo').innerHTML = '<i class="bi bi-person-plus me-2"></i>Novo Cliente';
    ['cliNmCliente','cliNrCpf','cliNrCnpj','cliNrDocumento','cliNrRG','cliDsNomeFantasia','cliNrInscEstadual','cliNrInscMunicipal','cliNrTelefoneFixo','cliNrCelular','cliDsEmail','cliDsSite','cliDsEndereco','cliNrNumero','cliDsComplemento','cliDsBairro','cliNrCEP','cliDtNascimento','cliDsSexo','cliIdCondicaoPagamento','cliDsObservacao','cliCodigoDisplay'].forEach(id => {
        document.getElementById(id).value = '';
    });
    document.getElementById('cliIdCliente').value = '0';
    document.getElementById('cliTpPessoa').value = 'F';
    definirAtivo('cliFlAtivo', true);
    limparCidade('cliIdCidade');
    toggleTipoPessoaCliente();
}

// Registro de cliente (mesmo formato da busca BuscarClientes e das linhas da
// tela: ClientesModel.Registro) no formulario de edicao.
function editarCliente(c) {
    document.getElementById('cliTitulo').innerHTML = '<i class="bi bi-pencil me-2"></i>Editar Cliente';
    document.getElementById('cliIdCliente').value = c.id;
    document.getElementById('cliCodigoDisplay').value = c.id;
    document.getElementById('cliNmCliente').value = c.nome || '';
    document.getElementById('cliTpPessoa').value = c.tipoPessoa || 'F';
    document.getElementById('cliNrCpf').value = mascaraCPF(c.cpf || '');
    document.getElementById('cliNrCnpj').value = mascaraCNPJ(c.cnpj || '');
    document.getElementById('cliNrDocumento').value = c.documento || '';
    document.getElementById('cliNrRG').value = mascaraRG(c.rg || '');
    document.getElementById('cliDsNomeFantasia').value = c.nomeFantasia || '';
    document.getElementById('cliNrInscEstadual').value = c.inscEstadual || '';
    document.getElementById('cliNrInscMunicipal').value = c.inscMunicipal || '';
    document.getElementById('cliNrTelefoneFixo').value = mascaraTelefone(c.telefoneFixo || '');
    document.getElementById('cliNrCelular').value = mascaraTelefone(c.celular || '');
    document.getElementById('cliDsSite').value = c.site || '';
    document.getElementById('cliDsEmail').value = c.email || '';
    document.getElementById('cliDsEndereco').value = c.end || '';
    document.getElementById('cliNrNumero').value = c.numero || '';
    document.getElementById('cliDsComplemento').value = c.complemento || '';
    document.getElementById('cliDsBairro').value = c.bairro || '';
    definirCidade('cliIdCidade', c.idCidade, c.cidadeNome);
    document.getElementById('cliNrCEP').value = mascaraCEP(c.cep || '');
    document.getElementById('cliDtNascimento').value = c.nasc || '';
    document.getElementById('cliDsSexo').value = c.sexo || '';
    // Condicao: a do cadastro, mesmo que hoje esteja inativa (fica com o nome).
    if (c.idCondicaoPagamento) selecionarNoSelect('cliIdCondicaoPagamento', c.idCondicaoPagamento, c.condicaoNome || String(c.idCondicaoPagamento));
    else document.getElementById('cliIdCondicaoPagamento').value = '';
    document.getElementById('cliDsObservacao').value = c.obs || '';
    definirAtivo('cliFlAtivo', c.ativo);
    toggleTipoPessoaCliente();
}

function urlClientes(handler) {
    return document.getElementById('modalCliente').dataset.urlCadastro + '?handler=' + handler;
}

// ===== Pesquisa de cliente (a lupa do campo de cliente de outras telas) =====
// opcoes: o que a tela faz com o registro (ver abrirPesquisaComCadastro no module.js).
// Telefone das listagens: o celular/WhatsApp ou, sem ele, o fixo.
function telefoneCliente(c) {
    const tel = c.celular || c.telefoneFixo;
    return tel ? mascaraTelefone(tel) : '-';
}

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
    inativos: 'Exibir Clientes Inativos',
    filtros: [
        { param: 'id', rotulo: 'ID', tipo: 'number', col: 2 },
        { param: 'nome', rotulo: 'Nome', col: 4 },
        { param: 'documento', rotulo: 'CPF / CNPJ / Documento', col: 3 },
        { param: 'email', rotulo: 'E-mail', col: 3 }
    ],
    colunas: ['Código', 'Nome', 'CPF / CNPJ', 'Telefone', 'Cidade'],
    celulas: c => ['<strong>' + c.id + '</strong>', escaparHtml(c.nome), documentoCliente(c),
                   telefoneCliente(c), escaparHtml(c.cidadeNome || '-')],
    modal: 'modalCliente',
    evento: 'cliente:salvo',
    preencher: editarCliente,
    limpar: limparFormCliente,
    urlExcluir: () => urlClientes('Excluir'),
    nome: c => c.nome
};

function abrirPesquisaCliente(opcoes) {
    abrirPesquisaComCadastro(PESQUISA_CLIENTE, opcoes);
}
