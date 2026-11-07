// Script do componente de cadastro de funcionario
// (Pages/Shared/Components/CadastroFuncionario/Default.cshtml), com a pesquisa
// e o cadastro rapido de cargo. Usado pela tela de Funcionarios e pelas
// pesquisas de funcionario (Venda, Agendamentos).

// ===== Mascaras (funcoes em wwwroot/js/module.js; no banco salva so numeros) =====
aplicarMascara('fCpf', mascaraCPF);
aplicarMascara('fCep', mascaraCEP);
aplicarMascara('fTel', mascaraTelefone);
aplicarMascara('fRg', mascaraRG);

function limparFormFuncionario() {
    document.getElementById('funTitulo').innerHTML = '<i class="bi bi-person-badge me-2"></i>Novo Funcionário';
    ['fId','fNome','fApelido','fCpf','fRg','fSexo','fTel','fEmail','fEnd','fBairro','fNumero','fComplemento','fCep','fNasc','fAdm','fDem','fCargo','fSal','fCom','fObs','fCodigoDisplay'].forEach(id => {
        document.getElementById(id).value = '';
    });
    document.getElementById('fId').value = '0';
    document.getElementById('fAtivo').value = 'true';
    limparCidade('fCidade');
}

function editarFunc(id,nome,cpf,tel,email,end,bairro,cidade,cep,nasc,adm,dem,idCargo,sal,com,obs,ativo,apelido,rg,numero,complemento,sexo) {
    document.getElementById('funTitulo').innerHTML = '<i class="bi bi-pencil me-2"></i>Editar Funcionário';
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

// ===== Cargo: pesquisa (a lupa do campo) e cadastro rapido =====
// Mesmo padrao das outras pesquisas: Selecionar coloca no funcionario, Editar
// e Novo abrem o cadastro rapido, a lixeira usa o Excluir da tela de Cargos.
function urlCargos(handler) {
    return document.getElementById('modalCargoRapido').dataset.urlCargos + '?handler=' + handler;
}

function abrirPesquisaCargo() {
    abrirPesquisa({
        chave: 'cargo',
        titulo: 'Pesquisar Cargo',
        url: () => urlCargos('BuscarCargos'),
        inativos: 'Exibir Cargos Inativos',
        filtros: [
            { param: 'id', rotulo: 'ID', tipo: 'number', col: 2 },
            { param: 'termo', rotulo: 'Cargo / Descrição', col: 6 }
        ],
        colunas: ['Código', 'Cargo', 'Descrição', 'Comissão (%)'],
        celulas: c => ['<strong>' + c.id + '</strong>', escaparHtml(c.nome), escaparHtml(c.desc || '-'),
                       c.comissao != null ? moeda(c.comissao) : '-'],
        selecionar: c => selecionarNoSelect('fCargo', c.id, c.nome),
        editar: c => abrirCadastroCargo(c),
        novo: () => abrirCadastroCargo(null),
        excluir: { url: () => urlCargos('Excluir'), nome: c => c.nome, depois: c => removerDoSelect('fCargo', c.id) }
    });
}

function abrirCadastroCargo(c) {
    document.getElementById('crMsg').innerHTML = '';
    document.getElementById('crTitulo').textContent = c ? 'Editar Cargo' : 'Cadastrar Cargo';
    document.getElementById('crId').value = c ? c.id : 0;
    document.getElementById('crNome').value = c ? c.nome : '';
    document.getElementById('crDesc').value = c ? (c.desc || '') : '';
    document.getElementById('crComissao').value = c && c.comissao != null ? c.comissao : '';
    abrirModalEmpilhado('modalCargoRapido');
}

// Edicao volta para a pesquisa atualizada; cargo novo ja fica selecionado.
async function salvarCargoRapido() {
    const id = parseInt(document.getElementById('crId').value) || 0;
    const nome = document.getElementById('crNome').value.trim();
    const desc = document.getElementById('crDesc').value.trim();
    const comissao = document.getElementById('crComissao').value;
    const msg = document.getElementById('crMsg');
    const btn = document.getElementById('btnSalvarCargoRapido');
    msg.innerHTML = '';
    if (!nome) { msg.innerHTML = alertaErro('Informe o nome do cargo.'); return; }

    btn.disabled = true;
    try {
        const data = await postJson(urlFuncionarios('CargoRapido'), {
            idCargo: id,
            nmCargo: nome,
            dsCargo: desc || null,
            vlComissaoPadrao: comissao ? parseFloat(comissao) : null
        });
        if (!data.sucesso) { msg.innerHTML = alertaErro(data.mensagem); return; }
        if (id) renomearNoSelect('fCargo', data.id, data.nome);
        concluirCadastro('modalCargoRapido', 'cargo', id > 0, () => selecionarNoSelect('fCargo', data.id, data.nome));
    } catch {
        msg.innerHTML = alertaErro('Erro ao salvar o cargo. Tente novamente.');
    } finally {
        btn.disabled = false;
    }
}

function urlFuncionarios(handler) {
    return document.getElementById('modalFuncionario').dataset.urlCadastro + '?handler=' + handler;
}

// Registro da pesquisa (BuscarFuncionarios) no formulario de edicao.
function editarFuncionarioPorRegistro(f) {
    editarFunc(
        f.id, f.nome || '', f.cpf || '', f.tel || '', f.email || '',
        f.end || '', f.bairro || '', (f.idCidade ?? ''), f.cep || '',
        f.nasc || '', f.adm || '', f.dem || '', (f.idCargo ?? ''),
        (f.sal ?? ''), (f.com ?? ''), f.obs || '', (f.ativo ? 'True' : 'False'),
        f.apelido || '', f.rg || '', f.numero || '', f.complemento || '', f.sexo || ''
    );
}

// ===== Pesquisa de funcionario (a lupa do campo de funcionario/profissional) =====
// opcoes: o que a tela faz com o registro (ver abrirPesquisaComCadastro no module.js).
const PESQUISA_FUNCIONARIO = {
    chave: 'funcionario',
    titulo: 'Pesquisar Funcionário',
    // Mesma busca da tela de Funcionarios (so ativos, sem diferenciar acento).
    url: () => urlFuncionarios('BuscarFuncionarios'),
    inativos: 'Exibir Funcionários Inativos',
    filtros: [
        { param: 'id', rotulo: 'ID', tipo: 'number', col: 2 },
        { param: 'nome', rotulo: 'Nome', col: 4 },
        { param: 'apelido', rotulo: 'Apelido', col: 3 },
        { param: 'cpf', rotulo: 'CPF', col: 3 }
    ],
    colunas: ['Código', 'Nome', 'Apelido', 'CPF', 'Cargo'],
    celulas: f => ['<strong>' + f.id + '</strong>', escaparHtml(f.nome), escaparHtml(f.apelido || '-'),
                   f.cpf ? mascaraCPF(f.cpf) : '-', escaparHtml(f.cargoNome || '-')],
    modal: 'modalFuncionario',
    evento: 'funcionario:salvo',
    preencher: editarFuncionarioPorRegistro,
    limpar: limparFormFuncionario,
    urlExcluir: () => urlFuncionarios('Excluir'),
    nome: f => f.nome
};

function abrirPesquisaFuncionario(opcoes, titulo = 'Pesquisar Funcionário') {
    abrirPesquisaComCadastro({ ...PESQUISA_FUNCIONARIO, titulo }, opcoes);
}
