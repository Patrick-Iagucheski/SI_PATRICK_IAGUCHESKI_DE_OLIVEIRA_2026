// Script da tela Funcionários (Pages/Gerais/Funcionarios.cshtml).

// Filtro rapido da lista (funcao em wwwroot/js/module.js).
filtrarTabela('#tblFuncionarios tbody tr', { texto: 'searchInput' });

// ===== Mascaras (funcoes em wwwroot/js/module.js; no banco salva so numeros) =====
aplicarMascara('fCpf', mascaraCPF);
aplicarMascara('fCep', mascaraCEP);
aplicarMascara('fTel', mascaraTelefone);
aplicarMascara('fRg', mascaraRG);

function limparForm() {
    document.getElementById('modalTitle').innerHTML = '<i class="bi bi-person-badge me-2"></i>Novo Funcionário';
    ['fId','fNome','fApelido','fCpf','fRg','fSexo','fTel','fEmail','fEnd','fBairro','fNumero','fComplemento','fCep','fNasc','fAdm','fDem','fCargo','fSal','fCom','fObs','fCodigoDisplay'].forEach(id => {
        document.getElementById(id).value = '';
    });
    document.getElementById('fId').value = '0';
    document.getElementById('fAtivo').value = 'true';
    limparCidade('fCidade');
}

function editarFunc(id,nome,cpf,tel,email,end,bairro,cidade,cep,nasc,adm,dem,idCargo,sal,com,obs,ativo,apelido,rg,numero,complemento,sexo) {
    document.getElementById('modalTitle').innerHTML = '<i class="bi bi-pencil me-2"></i>Editar Funcionário';
    document.getElementById('fId').value = id;
    document.getElementById('fCodigoDisplay').value = id;
    document.getElementById('fNome').value = nome;
    document.getElementById('fApelido').value = apelido;
    document.getElementById('fCpf').value = mascaraCPF(cpf || '');
    document.getElementById('fRg').value = mascaraRG(rg || '');
    document.getElementById('fSexo').value = sexo || '';
    document.getElementById('fTel').value = mascaraTelefone(tel || '');
    document.getElementById('fEmail').value = email;
    document.getElementById('fEnd').value = end;
    document.getElementById('fBairro').value = bairro;
    document.getElementById('fNumero').value = numero;
    document.getElementById('fComplemento').value = complemento;
    definirCidade('fCidade', cidade);
    document.getElementById('fCep').value = mascaraCEP(cep || '');
    document.getElementById('fNasc').value = nasc;
    document.getElementById('fAdm').value = adm;
    document.getElementById('fDem').value = dem;
    document.getElementById('fCargo').value = idCargo;
    document.getElementById('fSal').value = sal;
    document.getElementById('fCom').value = com;
    document.getElementById('fObs').value = obs;
    document.getElementById('fAtivo').value = ativo === 'True' ? 'true' : 'false';
}

// ===== Cadastro rapido de Cargo (AJAX) - sem sair da tela de Funcionarios =====
async function salvarCargoRapido() {
    const nome = document.getElementById('crNome').value.trim();
    const desc = document.getElementById('crDesc').value.trim();
    const comissao = document.getElementById('crComissao').value;
    const msg = document.getElementById('crMsg');
    const btn = document.getElementById('btnSalvarCargoRapido');
    msg.innerHTML = '';

    if (!nome) {
        msg.innerHTML = alertaErro('Informe o nome do cargo.');
        return;
    }

    btn.disabled = true;
    try {
        const data = await postJson('?handler=CargoRapido', {
            nmCargo: nome,
            dsCargo: desc || null,
            vlComissaoPadrao: comissao ? parseFloat(comissao) : null
        });

        if (!data.sucesso) {
            msg.innerHTML = alertaErro(data.mensagem);
            return;
        }

        // Adiciona o novo cargo no dropdown e ja o seleciona automaticamente
        const sel = document.getElementById('fCargo');
        const opt = document.createElement('option');
        opt.value = data.id;
        opt.textContent = data.nome;
        sel.appendChild(opt);
        sel.value = data.id;

        // Limpa e fecha o modal de cadastro rapido
        document.getElementById('crNome').value = '';
        document.getElementById('crDesc').value = '';
        document.getElementById('crComissao').value = '';
        bootstrap.Modal.getInstance(document.getElementById('modalCargoRapido')).hide();
    } catch (e) {
        msg.innerHTML = '<div class="alert alert-danger py-1 px-2 small mb-2">Erro ao salvar o cargo. Tente novamente.</div>';
    } finally {
        btn.disabled = false;
    }
}

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
        '<td><strong>' + (f.nome || '') + '</strong></td>' +
        '<td>' + mascaraCPF(f.cpf || '') + '</td>' +
        '<td>' + (f.cargoNome || '-') + '</td>' +
        '<td>' + (f.tel ? mascaraTelefone(f.tel) : '-') + '</td>' +
        '<td>' + adm + '</td>' +
        '<td>' + com + '</td>' +
        '<td>' + statusBadge(f.ativo) + '</td>' +
        '<td class="text-center">' +
            '<button class="btn btn-icon btn-outline-primary" title="Editar" onclick="abrirEdicao(' + i + ')"><i class="bi bi-pencil"></i></button> ' +
            '<form method="post" action="?handler=Excluir" class="d-inline" onsubmit="return confirm(\'Deseja realmente excluir este funcionario?\')">' +
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
    const f = resultadosBusca[i];
    editarFunc(
        f.id, f.nome || '', f.cpf || '', f.tel || '', f.email || '',
        f.end || '', f.bairro || '', (f.idCidade ?? ''), f.cep || '',
        f.nasc || '', f.adm || '', f.dem || '', (f.idCargo ?? ''),
        (f.sal ?? ''), (f.com ?? ''), f.obs || '', (f.ativo ? 'True' : 'False'),
        f.apelido || '', f.rg || '', f.numero || '', f.complemento || '', f.sexo || ''
    );
    bootstrap.Modal.getOrCreateInstance(document.getElementById('modalFuncionario')).show();
}

