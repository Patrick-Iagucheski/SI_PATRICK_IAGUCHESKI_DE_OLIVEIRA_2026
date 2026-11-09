// Script do componente de cadastro de transportadora
// (Pages/Shared/Components/CadastroTransportador/Default.cshtml).
// Usado pela tela de Transportadores e pela pesquisa de transportadora (lupa do
// campo de transportadora da Compra).

// ===== Mascaras (funcoes em wwwroot/js/module.js; no banco salva so numeros) =====
aplicarMascara('trNrCpf', mascaraCPF);
aplicarMascara('trNrCNPJ', mascaraCNPJ);
aplicarMascara('trNrCEP', mascaraCEP);
aplicarMascara('trNrTelefoneFixo', mascaraTelefone);
aplicarMascara('trNrCelular', mascaraTelefone);
aplicarMascara('trNrRG', mascaraRG);

// ===== Tipo de Pessoa: alterna documento e os campos especificos =====
function toggleTipoPessoaTransportador() {
    const tipo = document.getElementById('trTpPessoa').value;
    const ehF = tipo === 'F';
    const ehJ = tipo === 'J';

    // Documento principal (CNPJ / CPF / Documento)
    document.getElementById('trWrapCnpj').classList.toggle('d-none', !ehJ);
    document.getElementById('trWrapCpf').classList.toggle('d-none', !ehF);
    document.getElementById('trWrapDoc').classList.toggle('d-none', !(tipo === 'E' || tipo === 'O'));
    document.getElementById('trLblDoc').textContent = tipo === 'E' ? 'Documento Internacional' : 'Documento';
    document.getElementById('trLblRazao').textContent = ehJ ? 'Razão Social *' : 'Nome / Razão Social *';

    // Nome Fantasia so na juridica, ao lado da Razao Social (que desce para a
    // linha dele). Escondido, o campo e limpo para nao gravar valor de outro tipo.
    document.getElementById('trWrapFantasia').classList.toggle('d-none', !ehJ);
    document.getElementById('trQuebraRazao').classList.toggle('d-none', !ehJ);
    if (!ehJ) document.getElementById('trDsNomeFantasia').value = '';

    // Slot A: Inscricao Estadual (J / E / O) ou RG (F)
    document.getElementById('trWrapInscEstadual').classList.toggle('d-none', ehF);
    document.getElementById('trWrapRG').classList.toggle('d-none', !ehF);

    // Slot B: Inscricao Municipal (J) ou Data de Nascimento (F / E / O)
    document.getElementById('trWrapInscMunicipal').classList.toggle('d-none', !ehJ);
    document.getElementById('trWrapDataNasc').classList.toggle('d-none', ehJ);

    // Obrigatorio so o documento visivel (CPF na fisica, CNPJ na juridica):
    // campo escondido com required travaria o envio do formulario.
    document.getElementById('trNrCpf').required = ehF;
    document.getElementById('trNrCNPJ').required = ehJ;
}

// ===== Validacao antes de enviar =====
document.querySelector('#modalTransportador form')?.addEventListener('submit', function (e) {
    const tipo = document.getElementById('trTpPessoa').value;
    const cnpj = document.getElementById('trNrCNPJ').value;
    const cpf = document.getElementById('trNrCpf').value;
    const email = document.getElementById('trDsEmail').value.trim();
    if (tipo === 'J' && cnpj.replace(/\D/g, '') && !cnpjValido(cnpj)) { e.preventDefault(); exibirErro(this, 'CNPJ inválido. Verifique os números informados.'); return; }
    if (tipo === 'F' && cpf.replace(/\D/g, '') && !cpfValido(cpf)) { e.preventDefault(); exibirErro(this, 'CPF inválido. Verifique os números informados.'); return; }
    if (tipo === 'F') {
        const rg = document.getElementById('trNrRG').value.replace(/\D/g, '');
        if (rg && rg.length !== 9) { e.preventDefault(); exibirErro(this, 'RG inválido. Informe 9 dígitos.'); return; }
    }
    if (tipo === 'J') {
        const ie = document.getElementById('trNrInscEstadual').value.replace(/\D/g, '');
        if (ie && (ie.length < 8 || ie.length > 13)) { e.preventDefault(); exibirErro(this, 'Inscrição Estadual deve ter de 8 a 13 dígitos.'); return; }
        const im = document.getElementById('trNrInscMunicipal').value.trim();
        if (im && (im.length < 7 || im.length > 15)) { e.preventDefault(); exibirErro(this, 'Inscrição Municipal deve ter de 7 a 15 caracteres.'); return; }
    }
    if (email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) { e.preventDefault(); exibirErro(this, 'E-mail inválido. Verifique o formato (exemplo: nome@dominio.com).'); return; }
});

function limparFormTransportador() {
    document.getElementById('trTitulo').innerHTML = '<i class="bi bi-truck-front me-2"></i>Nova Transportadora';
    ['trDsRazaoSocial','trDsNomeFantasia','trNrCNPJ','trNrCpf','trNrDocumento','trNrInscEstadual','trNrInscMunicipal','trNrRG','trDtNascimento','trDsObservacao','trNrCEP','trDsEndereco','trNrNumero','trDsComplemento','trDsBairro','trNrTelefoneFixo','trNrCelular','trDsEmail','trDsSite','trCodigoDisplay'].forEach(id => {
        document.getElementById(id).value = '';
    });
    document.getElementById('trId').value = '0';
    document.getElementById('trTpPessoa').value = 'J';
    definirAtivo('trFlAtivo', true);
    limparCidade('trIdCidade');
    toggleTipoPessoaTransportador();
}

function editarTransportador(t) {
    document.getElementById('trTitulo').innerHTML = '<i class="bi bi-pencil me-2"></i>Editar Transportadora';
    document.getElementById('trId').value = t.id;
    document.getElementById('trCodigoDisplay').value = t.id;
    document.getElementById('trTpPessoa').value = t.tipoPessoa || 'J';
    document.getElementById('trDsRazaoSocial').value = t.razaoSocial || '';
    document.getElementById('trDsNomeFantasia').value = t.nomeFantasia || '';
    document.getElementById('trNrCNPJ').value = mascaraCNPJ(t.cnpj || '');
    document.getElementById('trNrCpf').value = mascaraCPF(t.cpf || '');
    document.getElementById('trNrDocumento').value = t.documento || '';
    document.getElementById('trNrInscEstadual').value = t.inscEstadual || '';
    document.getElementById('trNrInscMunicipal').value = t.inscMunicipal || '';
    document.getElementById('trNrRG').value = mascaraRG(t.rg || '');
    document.getElementById('trDtNascimento').value = t.dataNascimento || '';
    document.getElementById('trDsEmail').value = t.email || '';
    document.getElementById('trDsObservacao').value = t.observacao || '';
    document.getElementById('trNrCEP').value = mascaraCEP(t.cep || '');
    document.getElementById('trDsEndereco').value = t.endereco || '';
    document.getElementById('trNrNumero').value = t.numero || '';
    document.getElementById('trDsComplemento').value = t.complemento || '';
    document.getElementById('trDsBairro').value = t.bairro || '';
    definirCidade('trIdCidade', t.idCidade, t.cidadeNome);
    document.getElementById('trNrTelefoneFixo').value = mascaraTelefone(t.telefoneFixo || '');
    document.getElementById('trNrCelular').value = mascaraTelefone(t.celular || '');
    document.getElementById('trDsSite').value = t.site || '';
    definirAtivo('trFlAtivo', t.ativo === true || t.ativo === 'True');
    toggleTipoPessoaTransportador();
}

// ===== Helpers AJAX =====
// Os handlers ficam em TransportadoresModel; a URL vem do componente para
// funcionar em qualquer tela (e sob o diretorio virtual do IIS).
function urlTransportadores(handler) {
    return document.getElementById('modalTransportador').dataset.urlTransportadores + '?handler=' + handler;
}

// ===== Pesquisa de transportadora (a lupa do campo de transportadora de outras telas) =====
// opcoes: o que a tela faz com o registro (ver abrirPesquisaComCadastro no module.js).
function documentoTransportador(t) {
    if (t.tipoPessoa === 'F') return t.cpf ? mascaraCPF(t.cpf) : '-';
    if (t.tipoPessoa === 'E' || t.tipoPessoa === 'O') return escaparHtml(t.documento || '-');
    return t.cnpj ? mascaraCNPJ(t.cnpj) : '-';
}

const PESQUISA_TRANSPORTADOR = {
    chave: 'transportador',
    titulo: 'Pesquisar Transportadora',
    // Mesma busca da tela de Transportadores (so ativas, sem diferenciar acento).
    url: () => urlTransportadores('BuscarTransportadores'),
    inativos: 'Exibir Transportadoras Inativas',
    filtros: [
        { param: 'id', rotulo: 'ID', tipo: 'number', col: 2 },
        { param: 'razao', rotulo: 'Razão Social', col: 4 },
        { param: 'fantasia', rotulo: 'Nome Fantasia', col: 3 },
        { param: 'documento', rotulo: 'CPF / CNPJ', col: 3 }
    ],
    colunas: ['Código', 'Razão Social', 'Nome Fantasia', 'CPF / CNPJ', 'Cidade'],
    celulas: t => ['<strong>' + t.id + '</strong>', escaparHtml(t.razaoSocial), escaparHtml(t.nomeFantasia || '-'),
                   documentoTransportador(t), escaparHtml(t.cidadeNome || '-')],
    modal: 'modalTransportador',
    evento: 'transportador:salvo',
    preencher: editarTransportador,
    limpar: limparFormTransportador,
    urlExcluir: () => urlTransportadores('Excluir'),
    nome: t => t.razaoSocial
};

function abrirPesquisaTransportador(opcoes) {
    abrirPesquisaComCadastro(PESQUISA_TRANSPORTADOR, opcoes);
}
