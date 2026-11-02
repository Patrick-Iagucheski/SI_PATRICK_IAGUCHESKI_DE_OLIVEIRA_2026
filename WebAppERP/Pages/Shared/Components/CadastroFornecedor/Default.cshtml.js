// Script do componente de cadastro de fornecedor
// (Pages/Shared/Components/CadastroFornecedor/Default.cshtml).
// Usado pela tela de Fornecedores e pela pesquisa de fornecedor da Compra.

// ===== Mascaras (funcoes em wwwroot/js/module.js; no banco salva so numeros) =====
aplicarMascara('formNrCpf', mascaraCPF);
aplicarMascara('formNrCNPJ', mascaraCNPJ);
aplicarMascara('formNrCEP', mascaraCEP);
aplicarMascara('formNrTelefoneFixo', mascaraTelefone);
aplicarMascara('formNrCelular', mascaraTelefone);
aplicarMascara('formNrWhatsApp', mascaraTelefone);
aplicarMascara('formNrRG', mascaraRG);

// ===== Tipo de Pessoa: alterna documento e os campos especificos =====
function toggleTipoPessoa() {
    const tipo = document.getElementById('formTpPessoa').value;
    const ehF = tipo === 'F';
    const ehJ = tipo === 'J';

    // Documento principal (CNPJ / CPF / Documento)
    document.getElementById('wrapCnpj').classList.toggle('d-none', tipo !== 'J');
    document.getElementById('wrapCpf').classList.toggle('d-none', tipo !== 'F');
    document.getElementById('wrapDoc').classList.toggle('d-none', !(tipo === 'E' || tipo === 'O'));
    document.getElementById('lblDoc').textContent = tipo === 'E' ? 'Documento Internacional' : 'Documento';
    document.getElementById('lblRazao').textContent = tipo === 'J' ? 'Razão Social *' : 'Nome / Razão Social *';

    // Slot A: Inscricao Estadual (J / E / O) ou RG (F)
    document.getElementById('wrapInscEstadual').classList.toggle('d-none', ehF);
    document.getElementById('wrapRG').classList.toggle('d-none', !ehF);

    // Slot B: Inscricao Municipal (J) ou Data de Nascimento (F / E / O)
    document.getElementById('wrapInscMunicipal').classList.toggle('d-none', !ehJ);
    document.getElementById('wrapDataNasc').classList.toggle('d-none', ehJ);

    // RG so e obrigatorio para Pessoa Fisica
    document.getElementById('formNrRG').required = ehF;
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
    if (tipo === 'J' && cnpj.replace(/\D/g, '') && !cnpjValido(cnpj)) { e.preventDefault(); alert('CNPJ inválido. Verifique os números informados.'); return; }
    if (tipo === 'F' && cpf.replace(/\D/g, '') && !cpfValido(cpf)) { e.preventDefault(); alert('CPF inválido. Verifique os números informados.'); return; }
    if (tipo === 'F') {
        const rg = document.getElementById('formNrRG').value.replace(/\D/g, '');
        if (rg.length !== 9) { e.preventDefault(); alert('Informe o RG com 9 dígitos (Pessoa Física).'); return; }
    }
    if (tipo === 'J') {
        const ie = document.getElementById('formNrInscEstadual').value.replace(/\D/g, '');
        if (ie && (ie.length < 8 || ie.length > 13)) { e.preventDefault(); alert('Inscrição Estadual deve ter de 8 a 13 dígitos.'); return; }
        const im = document.getElementById('formNrInscMunicipal').value.trim();
        if (im && (im.length < 7 || im.length > 15)) { e.preventDefault(); alert('Inscrição Municipal deve ter de 7 a 15 caracteres.'); return; }
    }
    if (email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) { e.preventDefault(); alert('E-mail inválido. Verifique o formato (exemplo: nome@dominio.com).'); return; }

    // Modo AJAX: grava sem recarregar a tela que abriu o cadastro.
    if (document.getElementById('modalFornecedor').dataset.modo === 'ajax') {
        e.preventDefault();
        salvarFornecedorAjax(this);
    }
});

function limparForm() {
    document.getElementById('modalTitle').innerHTML = '<i class="bi bi-truck me-2"></i>Novo Fornecedor';
    ['formDsRazaoSocial','formDsNomeFantasia','formDsContatoPrincipal','formNrCNPJ','formNrCpf','formNrDocumento','formNrInscEstadual','formNrInscMunicipal','formNrRG','formDtNascimento','formDsObservacao','formNrCEP','formDsEndereco','formNrNumero','formDsComplemento','formDsBairro','formIdCondicaoPagamento','formNrTelefoneFixo','formNrCelular','formNrWhatsApp','formDsEmail','formDsBanco','formNrAgencia','formTpConta','formNrConta','formNrDigitoConta','formTpChavePix','formDsChavePix','formCodigoDisplay'].forEach(id => {
        document.getElementById(id).value = '';
    });
    document.getElementById('formId').value = '0';
    document.getElementById('formTpPessoa').value = 'J';
    document.getElementById('formFlAtivo').value = 'true';
    document.getElementById('fornMsg').innerHTML = '';
    limparCidade('formIdCidade');
    toggleTipoPessoa();
}

function editarFornecedor(f) {
    document.getElementById('modalTitle').innerHTML = '<i class="bi bi-pencil me-2"></i>Editar Fornecedor';
    document.getElementById('formId').value = f.id;
    document.getElementById('formCodigoDisplay').value = f.id;
    document.getElementById('formTpPessoa').value = f.tipoPessoa || 'J';
    document.getElementById('formDsRazaoSocial').value = f.razaoSocial || '';
    document.getElementById('formDsNomeFantasia').value = f.nomeFantasia || '';
    document.getElementById('formDsContatoPrincipal').value = f.contatoPrincipal || '';
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
    document.getElementById('formNrWhatsApp').value = mascaraTelefone(f.whatsApp || '');
    document.getElementById('formDsBanco').value = f.banco || '';
    document.getElementById('formNrAgencia').value = f.agencia || '';
    document.getElementById('formTpConta').value = f.tipoConta || '';
    document.getElementById('formNrConta').value = f.conta || '';
    document.getElementById('formNrDigitoConta').value = f.digitoConta || '';
    document.getElementById('formTpChavePix').value = f.tipoChavePix || '';
    document.getElementById('formDsChavePix').value = f.chavePix || '';
    aplicarMascaraPix();
    document.getElementById('formFlAtivo').value = (f.ativo === true || f.ativo === 'True') ? 'true' : 'false';
    document.getElementById('fornMsg').innerHTML = '';
    toggleTipoPessoa();
}

// ===== Helpers AJAX =====
// Os handlers ficam em FornecedoresModel; a URL vem do componente para
// funcionar em qualquer tela (e sob o diretorio virtual do IIS).
function urlFornecedores(handler) {
    return document.getElementById('modalFornecedor').dataset.urlFornecedores + '?handler=' + handler;
}

// Condicao cadastrada pelo + do campo: entra no dropdown ja selecionada
// (o dropdown so lista condicoes ativas).
document.addEventListener('condicao:salva', e => {
    const sel = document.getElementById('formIdCondicaoPagamento');
    if (!sel || !e.detail.ativo) return;
    let opt = sel.querySelector('option[value="' + e.detail.id + '"]');
    if (!opt) {
        opt = document.createElement('option');
        opt.value = e.detail.id;
        sel.appendChild(opt);
    }
    opt.textContent = e.detail.nome;
    sel.value = e.detail.id;
});

// Grava o fornecedor e avisa a tela de origem pelo evento 'fornecedor:salvo'.
async function salvarFornecedorAjax(form) {
    const msg = document.getElementById('fornMsg');
    const btn = form.querySelector('button[type="submit"]');
    msg.innerHTML = '';
    btn.disabled = true;
    try {
        const resp = await fetch(urlFornecedores('SalvarAjax'), { method: 'POST', body: new FormData(form) });
        const data = await resp.json();
        if (!data.sucesso) { msg.innerHTML = alertaErro(data.mensagem); msg.scrollIntoView({ block: 'nearest' }); return; }
        bootstrap.Modal.getInstance(document.getElementById('modalFornecedor')).hide();
        document.dispatchEvent(new CustomEvent('fornecedor:salvo', { detail: data }));
    } catch (e) {
        msg.innerHTML = alertaErro('Erro ao salvar o fornecedor. Tente novamente.');
        msg.scrollIntoView({ block: 'nearest' });
    } finally { btn.disabled = false; }
}

