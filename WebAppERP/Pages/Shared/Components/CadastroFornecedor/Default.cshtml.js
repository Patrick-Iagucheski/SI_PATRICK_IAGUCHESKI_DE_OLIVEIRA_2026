// Script do componente de cadastro de fornecedor
// (Pages/Shared/Components/CadastroFornecedor/Default.cshtml).
// Usado pela tela de Fornecedores e pela pesquisa de fornecedor (lupa do
// fornecedor da Compra e de Contas a Pagar).

// ===== Mascaras (funcoes em wwwroot/js/module.js; no banco salva so numeros) =====
aplicarMascara('formNrCpf', mascaraCPF);
aplicarMascara('formNrCNPJ', mascaraCNPJ);
aplicarMascara('formNrCEP', mascaraCEP);
aplicarMascara('formNrTelefoneFixo', mascaraTelefone);
aplicarMascara('formNrCelular', mascaraTelefone);
aplicarMascara('formNrRG', mascaraRG);

// ===== Tipo de Pessoa: alterna documento e os campos especificos =====
function toggleTipoPessoaFornecedor() {
    const tipo = document.getElementById('formTpPessoa').value;
    const ehF = tipo === 'F';
    const ehJ = tipo === 'J';

    // Documento principal (CNPJ / CPF / Documento)
    document.getElementById('wrapCnpj').classList.toggle('d-none', tipo !== 'J');
    document.getElementById('wrapCpf').classList.toggle('d-none', tipo !== 'F');
    document.getElementById('wrapDoc').classList.toggle('d-none', !(tipo === 'E' || tipo === 'O'));
    document.getElementById('lblDoc').textContent = tipo === 'E' ? 'Documento Internacional' : 'Documento';
    document.getElementById('lblRazao').textContent = tipo === 'J' ? 'Razão Social *' : 'Nome / Razão Social *';

    // Nome Fantasia so na juridica, ao lado da Razao Social (que desce para a
    // linha dele). Escondido, o campo e limpo para nao gravar valor de outro tipo.
    document.getElementById('wrapFantasia').classList.toggle('d-none', !ehJ);
    document.getElementById('formQuebraRazao').classList.toggle('d-none', !ehJ);
    if (!ehJ) document.getElementById('formDsNomeFantasia').value = '';

    // Slot A: Inscricao Estadual (J / E / O) ou RG (F)
    document.getElementById('wrapInscEstadual').classList.toggle('d-none', ehF);
    document.getElementById('wrapRG').classList.toggle('d-none', !ehF);

    // Slot B: Inscricao Municipal (J) ou Data de Nascimento (F / E / O)
    document.getElementById('wrapInscMunicipal').classList.toggle('d-none', !ehJ);
    document.getElementById('wrapDataNasc').classList.toggle('d-none', ehJ);

    // Obrigatorio so o documento visivel (CPF na fisica, CNPJ na juridica):
    // campo escondido com required travaria o envio do formulario.
    document.getElementById('formNrCpf').required = ehF;
    document.getElementById('formNrCNPJ').required = ehJ;
}

// ===== Mascara da chave PIX conforme o tipo =====
function aplicarMascaraPix() {
    const t = document.getElementById('formTpChavePix').value;
    const el = document.getElementById('formDsChavePix');
    if (t === 'CPF') el.value = mascaraCPF(el.value);
    else if (t === 'CNPJ') el.value = mascaraCNPJ(el.value);
    else if (t === 'CELULAR') el.value = mascaraTelefone(el.value);
}
document.getElementById('formDsChavePix')?.addEventListener('input', aplicarMascaraPix);

// ===== Validacao antes de enviar =====
document.querySelector('#modalFornecedor form')?.addEventListener('submit', function (e) {
    const tipo = document.getElementById('formTpPessoa').value;
    const cnpj = document.getElementById('formNrCNPJ').value;
    const cpf = document.getElementById('formNrCpf').value;
    const email = document.getElementById('formDsEmail').value.trim();
    if (tipo === 'J' && cnpj.replace(/\D/g, '') && !cnpjValido(cnpj)) { e.preventDefault(); exibirErro(this, 'CNPJ inválido. Verifique os números informados.'); return; }
    if (tipo === 'F' && cpf.replace(/\D/g, '') && !cpfValido(cpf)) { e.preventDefault(); exibirErro(this, 'CPF inválido. Verifique os números informados.'); return; }
    if (tipo === 'F') {
        const rg = document.getElementById('formNrRG').value.replace(/\D/g, '');
        if (rg && rg.length !== 9) { e.preventDefault(); exibirErro(this, 'RG inválido. Informe 9 dígitos.'); return; }
    }
    if (tipo === 'J') {
        const ie = document.getElementById('formNrInscEstadual').value.replace(/\D/g, '');
        if (ie && (ie.length < 8 || ie.length > 13)) { e.preventDefault(); exibirErro(this, 'Inscrição Estadual deve ter de 8 a 13 dígitos.'); return; }
        const im = document.getElementById('formNrInscMunicipal').value.trim();
        if (im && (im.length < 7 || im.length > 15)) { e.preventDefault(); exibirErro(this, 'Inscrição Municipal deve ter de 7 a 15 caracteres.'); return; }
    }
    if (email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) { e.preventDefault(); exibirErro(this, 'E-mail inválido. Verifique o formato (exemplo: nome@dominio.com).'); return; }
});

function limparFormFornecedor() {
    document.getElementById('fornTitulo').innerHTML = '<i class="bi bi-truck me-2"></i>Novo Fornecedor';
    ['formDsRazaoSocial','formDsNomeFantasia','formNrCNPJ','formNrCpf','formNrDocumento','formNrInscEstadual','formNrInscMunicipal','formNrRG','formDtNascimento','formDsObservacao','formNrCEP','formDsEndereco','formNrNumero','formDsComplemento','formDsBairro','formIdCondicaoPagamento','formNrTelefoneFixo','formNrCelular','formDsEmail','formDsSite','formDsBanco','formNrAgencia','formTpConta','formNrConta','formNrDigitoConta','formTpChavePix','formDsChavePix','formCodigoDisplay'].forEach(id => {
        document.getElementById(id).value = '';
    });
    document.getElementById('formId').value = '0';
    document.getElementById('formTpPessoa').value = 'J';
    definirAtivo('formFlAtivo', true);
    limparCidade('formIdCidade');
    toggleTipoPessoaFornecedor();
}

function editarFornecedor(f) {
    document.getElementById('fornTitulo').innerHTML = '<i class="bi bi-pencil me-2"></i>Editar Fornecedor';
    document.getElementById('formId').value = f.id;
    document.getElementById('formCodigoDisplay').value = f.id;
    document.getElementById('formTpPessoa').value = f.tipoPessoa || 'J';
    document.getElementById('formDsRazaoSocial').value = f.razaoSocial || '';
    document.getElementById('formDsNomeFantasia').value = f.nomeFantasia || '';
    document.getElementById('formNrCNPJ').value = mascaraCNPJ(f.cnpj || '');
    document.getElementById('formNrCpf').value = mascaraCPF(f.cpf || '');
    document.getElementById('formNrDocumento').value = f.documento || '';
    document.getElementById('formNrInscEstadual').value = f.inscEstadual || '';
    document.getElementById('formNrInscMunicipal').value = f.inscMunicipal || '';
    document.getElementById('formNrRG').value = mascaraRG(f.rg || '');
    document.getElementById('formDtNascimento').value = f.dataNascimento || '';
    document.getElementById('formDsEmail').value = f.email || '';
    document.getElementById('formDsObservacao').value = f.observacao || '';
    document.getElementById('formNrCEP').value = mascaraCEP(f.cep || '');
    document.getElementById('formDsEndereco').value = f.endereco || '';
    document.getElementById('formNrNumero').value = f.numero || '';
    document.getElementById('formDsComplemento').value = f.complemento || '';
    document.getElementById('formDsBairro').value = f.bairro || '';
    definirCidade('formIdCidade', f.idCidade, f.cidadeNome);
    document.getElementById('formIdCondicaoPagamento').value = f.idCondicaoPagamento || '';
    document.getElementById('formNrTelefoneFixo').value = mascaraTelefone(f.telefoneFixo || '');
    document.getElementById('formNrCelular').value = mascaraTelefone(f.celular || '');
    document.getElementById('formDsSite').value = f.site || '';
    document.getElementById('formDsBanco').value = f.banco || '';
    document.getElementById('formNrAgencia').value = f.agencia || '';
    document.getElementById('formTpConta').value = f.tipoConta || '';
    document.getElementById('formNrConta').value = f.conta || '';
    document.getElementById('formNrDigitoConta').value = f.digitoConta || '';
    document.getElementById('formTpChavePix').value = f.tipoChavePix || '';
    document.getElementById('formDsChavePix').value = f.chavePix || '';
    aplicarMascaraPix();
    definirAtivo('formFlAtivo', f.ativo === true || f.ativo === 'True');
    toggleTipoPessoaFornecedor();
}

// ===== Helpers AJAX =====
// Os handlers ficam em FornecedoresModel; a URL vem do componente para
// funcionar em qualquer tela (e sob o diretorio virtual do IIS).
function urlFornecedores(handler) {
    return document.getElementById('modalFornecedor').dataset.urlFornecedores + '?handler=' + handler;
}

// ===== Pesquisa de fornecedor (a lupa do campo de fornecedor de outras telas) =====
// opcoes: o que a tela faz com o registro (ver abrirPesquisaComCadastro no module.js).
function documentoFornecedor(f) {
    if (f.tipoPessoa === 'F') return f.cpf ? mascaraCPF(f.cpf) : '-';
    if (f.tipoPessoa === 'E' || f.tipoPessoa === 'O') return escaparHtml(f.documento || '-');
    return f.cnpj ? mascaraCNPJ(f.cnpj) : '-';
}

const PESQUISA_FORNECEDOR = {
    chave: 'fornecedor',
    titulo: 'Pesquisar Fornecedor',
    // Mesma busca da tela de Fornecedores (so ativos, sem diferenciar acento).
    url: () => urlFornecedores('BuscarFornecedores'),
    inativos: 'Exibir Fornecedores Inativos',
    filtros: [
        { param: 'id', rotulo: 'ID', tipo: 'number', col: 2 },
        { param: 'razao', rotulo: 'Razão Social', col: 4 },
        { param: 'fantasia', rotulo: 'Nome Fantasia', col: 3 },
        { param: 'documento', rotulo: 'CPF / CNPJ', col: 3 }
    ],
    colunas: ['Código', 'Razão Social', 'Nome Fantasia', 'CPF / CNPJ', 'Cidade'],
    celulas: f => ['<strong>' + f.id + '</strong>', escaparHtml(f.razaoSocial), escaparHtml(f.nomeFantasia || '-'),
                   documentoFornecedor(f), escaparHtml(f.cidadeNome || '-')],
    modal: 'modalFornecedor',
    evento: 'fornecedor:salvo',
    preencher: editarFornecedor,
    limpar: limparFormFornecedor,
    urlExcluir: () => urlFornecedores('Excluir'),
    nome: f => f.razaoSocial
};

function abrirPesquisaFornecedor(opcoes) {
    abrirPesquisaComCadastro(PESQUISA_FORNECEDOR, opcoes);
}
