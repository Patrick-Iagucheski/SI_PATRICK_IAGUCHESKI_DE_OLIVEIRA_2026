// Script do componente CampoCidade (Pages/Shared/Components/CampoCidade/Default.cshtml).
// Incluido pelo proprio componente, uma vez por pagina. Cuida de:
//  - campos de cidade, estado e pais: <select data-pesquisa> que o module.js
//    mostra como campo de selecao com lupa (abre a pesquisa);
//  - pesquisa de cidade / estado / pais, com Editar, Excluir, Selecionar e
//    Novo, e o cadastro de cada um;
//  - definirCidade / limparCidade, usados pelo JS de cada tela ao editar.
// Handlers no servidor: Pages/Gerais/Localidades.
// Roda antes do bootstrap.bundle (fica no meio do <body>): so usa o
// bootstrap dentro dos eventos, nunca no carregamento.

// Modal dentro de outro modal nao abre direito: leva os cadastros rapidos
// para o fim do <body>.
(function () {
    const bloco = document.getElementById('localidadeModais');
    if (bloco) document.body.appendChild(bloco);
})();

// ===== Helpers =====
function urlLocalidades(handler) {
    return document.getElementById('localidadeModais').dataset.urlLocalidades + '?handler=' + handler;
}

// Excluir da tela de cadastro de cada um (mesmas regras do Business).
function urlExcluirLocalidade(tela) {
    return document.getElementById('localidadeModais').dataset['url' + tela] + '?handler=Excluir';
}

// Qual handler cada tipo de campo consulta. Cidade e estado exibem a UF junto
// ("Curitiba / PR"); pais so o nome.
const LOCALIDADE = {
    cidade: { handler: 'BuscarCidades', comUF: true },
    estado: { handler: 'BuscarEstados', comUF: true },
    pais:   { handler: 'BuscarPaises',  comUF: false }
};

function textoLocalidade(tipo, reg) {
    return reg.nome + (LOCALIDADE[tipo].comUF && reg.detalhe ? ' / ' + reg.detalhe : '');
}

// ===== API usada pelas telas =====
// Preenche o campo de cidade (id do <select>). Sem o nome, busca pelo id.
async function definirCidade(id, idCidade, nome) {
    const sel = document.getElementById(id);
    if (!sel) return;
    if (!idCidade) { sel.value = ''; return; }
    selecionarNoSelect(id, idCidade, nome || String(idCidade));
    if (!nome) {
        try {
            const regs = await (await fetch(urlLocalidades('BuscarCidades') + '&id=' + encodeURIComponent(idCidade))).json();
            if (regs.length) renomearNoSelect(id, idCidade, textoLocalidade('cidade', regs[0]));
        } catch { /* fica so o id */ }
    }
}

function limparCidade(id) {
    definirCidade(id, '', '');
}

// Campos que abriram cada pesquisa (recebem o registro escolhido ou gravado).
// Estado e pais: por padrao os dos cadastros de cidade e de estado; as telas
// de Cidades, Estados e Veiculos passam o proprio campo.
let campoCidadeAtivo = null;
let campoEstadoAtivo = 'cidEstado';
let campoPaisAtivo = 'estPais';

// Coloca o registro no campo (<select>) de destino.
function selecionarLocalidade(campo, tipo, reg) {
    selecionarNoSelect(campo, reg.id, textoLocalidade(tipo, reg));
}

// ===== Pesquisas (a lupa de cada campo) =====
// Cada uma tem o seu modal (abrirPesquisa, no module.js), um por cima do
// outro: Cidade > Estado > Pais. Selecionar preenche o campo de quem chamou;
// Editar e Novo abrem o cadastro por cima da pesquisa.
const elLocalidade = id => document.getElementById(id);

function abrirPesquisaCidade(id) {
    campoCidadeAtivo = id;
    abrirPesquisa({
        chave: 'cidade',
        titulo: 'Pesquisar Cidade',
        url: () => urlLocalidades('BuscarCidades'),
        filtros: [
            { param: 'id', rotulo: 'ID', tipo: 'number', col: 2 },
            { param: 'termo', rotulo: 'Cidade', col: 6 },
            { param: 'uf', rotulo: 'UF', col: 2 }
        ],
        colunas: ['Código', 'Cidade', 'Estado', 'UF'],
        celulas: c => ['<strong>' + c.id + '</strong>', escaparHtml(c.nome), escaparHtml(c.estado || '-'), escaparHtml(c.detalhe || '-')],
        selecionar: c => selecionarLocalidade(campoCidadeAtivo, 'cidade', c),
        editar: c => abrirCadastroCidade(c),
        novo: () => abrirCadastroCidade(null),
        excluir: {
            url: () => urlExcluirLocalidade('Cidades'),
            nome: c => textoLocalidade('cidade', c),
            depois: c => removerDoSelect(campoCidadeAtivo, c.id)
        }
    });
}

function abrirPesquisaEstado(campo = 'cidEstado') {
    campoEstadoAtivo = campo;
    abrirPesquisa({
        chave: 'estado',
        titulo: 'Pesquisar Estado',
        url: () => urlLocalidades('BuscarEstados'),
        filtros: [
            { param: 'id', rotulo: 'ID', tipo: 'number', col: 2 },
            { param: 'termo', rotulo: 'Estado / UF', col: 6 }
        ],
        colunas: ['Código', 'Estado', 'UF', 'País'],
        celulas: e => ['<strong>' + e.id + '</strong>', escaparHtml(e.nome), escaparHtml(e.detalhe || '-'), escaparHtml(e.pais || '-')],
        selecionar: e => selecionarLocalidade(campoEstadoAtivo, 'estado', e),
        editar: e => abrirCadastroEstado(e),
        novo: () => abrirCadastroEstado(null),
        excluir: {
            url: () => urlExcluirLocalidade('Estados'),
            nome: e => textoLocalidade('estado', e),
            depois: e => removerDoSelect(campoEstadoAtivo, e.id)
        }
    });
}

function abrirPesquisaPais(campo = 'estPais') {
    campoPaisAtivo = campo;
    abrirPesquisa({
        chave: 'pais',
        titulo: 'Pesquisar País',
        url: () => urlLocalidades('BuscarPaises'),
        filtros: [
            { param: 'id', rotulo: 'ID', tipo: 'number', col: 2 },
            { param: 'termo', rotulo: 'País / Sigla', col: 6 }
        ],
        colunas: ['Código', 'País', 'Sigla', 'DDI', 'Moeda'],
        celulas: p => ['<strong>' + p.id + '</strong>', escaparHtml(p.nome), escaparHtml(p.detalhe || '-'),
                       escaparHtml(p.ddi || '-'), escaparHtml(p.moeda || '-')],
        selecionar: p => selecionarLocalidade(campoPaisAtivo, 'pais', p),
        editar: p => abrirCadastroPais(p),
        novo: () => abrirCadastroPais(null),
        excluir: {
            url: () => urlExcluirLocalidade('Paises'),
            nome: p => p.nome,
            depois: p => removerDoSelect(campoPaisAtivo, p.id)
        }
    });
}

// ===== Cadastros (reg = registro da pesquisa para editar, null para novo) =====
function abrirCadastroCidade(c) {
    elLocalidade('cidMsg').innerHTML = '';
    elLocalidade('cidTitulo').textContent = c ? 'Editar Cidade' : 'Cadastrar Cidade';
    elLocalidade('cidId').value = c ? c.id : 0;
    elLocalidade('cidNome').value = c ? c.nome : '';
    if (c && c.idEstado) selecionarNoSelect('cidEstado', c.idEstado, c.estado + (c.detalhe ? ' / ' + c.detalhe : ''));
    else elLocalidade('cidEstado').value = '';
    abrirModalEmpilhado('modalCidadeRapido');
}

function abrirCadastroEstado(e) {
    elLocalidade('estMsg').innerHTML = '';
    elLocalidade('estTitulo').textContent = e ? 'Editar Estado' : 'Cadastrar Estado';
    elLocalidade('estId').value = e ? e.id : 0;
    elLocalidade('estNome').value = e ? e.nome : '';
    elLocalidade('estUF').value = e ? (e.detalhe || '') : '';
    if (e && e.idPais) selecionarNoSelect('estPais', e.idPais, e.pais || String(e.idPais));
    else elLocalidade('estPais').value = '';
    abrirModalEmpilhado('modalEstadoRapido');
}

function abrirCadastroPais(p) {
    elLocalidade('paisMsg').innerHTML = '';
    elLocalidade('paisTitulo').textContent = p ? 'Editar País' : 'Cadastrar País';
    elLocalidade('paisId').value = p ? p.id : 0;
    elLocalidade('paisNome').value = p ? p.nome : '';
    elLocalidade('paisSigla').value = p ? (p.detalhe || '') : '';
    elLocalidade('paisDDI').value = p ? (p.ddi || '') : '';
    elLocalidade('paisMoeda').value = p ? (p.moeda || '') : '';
    abrirModalEmpilhado('modalPaisRapido');
}

// Depois de gravar: concluirCadastro (module.js) volta para a pesquisa na
// edicao ou seleciona o novo no campo de quem chamou.

// ===== Gravacao de Cidade =====
async function salvarCidadeRapido() {
    const id = parseInt(elLocalidade('cidId').value) || 0;
    const nome = elLocalidade('cidNome').value.trim();
    const idEstado = elLocalidade('cidEstado').value;
    const msg = elLocalidade('cidMsg');
    const btn = elLocalidade('btnSalvarCidadeRapido');
    msg.innerHTML = '';
    if (!nome) { msg.innerHTML = alertaErro('Informe o nome da cidade.'); return; }
    if (!idEstado) { msg.innerHTML = alertaErro('Selecione o estado.'); return; }
    btn.disabled = true;
    try {
        const data = await postJson(urlLocalidades('CidadeRapido'), { idCidade: id, nmCidade: nome, idEstado: parseInt(idEstado) });
        if (!data.sucesso) { msg.innerHTML = alertaErro(data.mensagem); return; }
        const reg = { id: data.id, nome: data.nome, detalhe: data.uf };
        // Cidade editada que esta no campo: atualiza o nome exibido.
        if (id) renomearNoSelect(campoCidadeAtivo, data.id, textoLocalidade('cidade', reg));
        concluirCadastro('modalCidadeRapido', 'cidade', id > 0, () => selecionarLocalidade(campoCidadeAtivo, 'cidade', reg));
    } catch { msg.innerHTML = alertaErro('Erro ao salvar a cidade. Tente novamente.'); }
    finally { btn.disabled = false; }
}

// ===== Gravacao de Estado =====
async function salvarEstadoRapido() {
    const id = parseInt(elLocalidade('estId').value) || 0;
    const nome = elLocalidade('estNome').value.trim();
    const uf = elLocalidade('estUF').value.trim().toUpperCase();
    const idPais = elLocalidade('estPais').value;
    const msg = elLocalidade('estMsg');
    const btn = elLocalidade('btnSalvarEstadoRapido');
    msg.innerHTML = '';
    if (!nome) { msg.innerHTML = alertaErro('Informe o nome do estado.'); return; }
    if (uf.length !== 2) { msg.innerHTML = alertaErro('Informe a UF (2 letras).'); return; }
    if (!idPais) { msg.innerHTML = alertaErro('Selecione o país.'); return; }
    btn.disabled = true;
    try {
        const data = await postJson(urlLocalidades('EstadoRapido'), { idEstado: id, sgUF: uf, nmEstado: nome, idPais: parseInt(idPais) });
        if (!data.sucesso) { msg.innerHTML = alertaErro(data.mensagem); return; }
        const reg = { id: data.id, nome: data.nome, detalhe: data.uf };
        if (id) renomearNoSelect(campoEstadoAtivo, data.id, textoLocalidade('estado', reg));
        concluirCadastro('modalEstadoRapido', 'estado', id > 0, () => selecionarLocalidade(campoEstadoAtivo, 'estado', reg));
    } catch { msg.innerHTML = alertaErro('Erro ao salvar o estado. Tente novamente.'); }
    finally { btn.disabled = false; }
}

// ===== Gravacao de Pais =====
async function salvarPaisRapido() {
    const id = parseInt(elLocalidade('paisId').value) || 0;
    const nome = elLocalidade('paisNome').value.trim();
    const sigla = elLocalidade('paisSigla').value.trim().toUpperCase();
    const ddi = elLocalidade('paisDDI').value.trim();
    const moeda = elLocalidade('paisMoeda').value.trim();
    const msg = elLocalidade('paisMsg');
    const btn = elLocalidade('btnSalvarPaisRapido');
    msg.innerHTML = '';
    if (!nome) { msg.innerHTML = alertaErro('Informe o nome do país.'); return; }
    if (!sigla) { msg.innerHTML = alertaErro('Informe a sigla do país.'); return; }
    btn.disabled = true;
    try {
        const data = await postJson(urlLocalidades('PaisRapido'), { idPais: id, nmPais: nome, sgPais: sigla, nrDDI: ddi || null, dsMoeda: moeda || null });
        if (!data.sucesso) { msg.innerHTML = alertaErro(data.mensagem); return; }
        const reg = { id: data.id, nome: data.nome };
        if (id) renomearNoSelect(campoPaisAtivo, data.id, data.nome);
        concluirCadastro('modalPaisRapido', 'pais', id > 0, () => selecionarLocalidade(campoPaisAtivo, 'pais', reg));
    } catch { msg.innerHTML = alertaErro('Erro ao salvar o país. Tente novamente.'); }
    finally { btn.disabled = false; }
}
