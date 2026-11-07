// Script do componente CampoCidade (Pages/Shared/Components/CampoCidade/Default.cshtml).
// Incluido pelo proprio componente, uma vez por pagina. Cuida de:
//  - droplist dos campos com data-droplist="cidade|estado|pais";
//  - pesquisa de cidade / estado / pais (o "+" dos campos), com Editar,
//    Selecionar e Novo, e o cadastro de cada um;
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
// Preenche o campo de cidade (id = input oculto). Sem o nome, busca pelo id.
async function definirCidade(id, idCidade, nome) {
    const oculto = document.getElementById(id);
    const texto = document.getElementById(id + 'Nome');
    if (!oculto || !texto) return;
    oculto.value = idCidade || '';
    texto.value = idCidade ? (nome || '') : '';
    if (idCidade && !nome) {
        try {
            const regs = await (await fetch(urlLocalidades('BuscarCidades') + '&id=' + encodeURIComponent(idCidade))).json();
            if (regs.length && oculto.value == idCidade) texto.value = textoLocalidade('cidade', regs[0]);
        } catch { /* fica so o id */ }
    }
}

function limparCidade(id) {
    definirCidade(id, '', '');
}

// Campo de cidade que abriu a pesquisa (recebe a cidade escolhida ou gravada).
let campoCidadeAtivo = null;

// ===== Droplist =====
// Ao focar ou digitar, a lista abre embaixo do campo filtrando pelo texto
// (vazio lista os primeiros cadastros). Setas + Enter escolhem, Esc fecha.
// O texto digitado so vale depois de escolher um item: ate la o id fica
// vazio, e ao sair do campo sem escolher o texto solto e limpo.
// Inicializa na primeira vez que o campo recebe foco, para valer tambem para
// campos que aparecem depois deste script.
function iniciarDroplist(campo) {
    const tipo = campo.dataset.droplist;
    const oculto = document.getElementById(campo.dataset.alvo);
    const lista = document.getElementById(campo.id + 'Lista');
    if (!LOCALIDADE[tipo] || !oculto || !lista) return;
    let timer = null;
    let regs = [];
    let ativo = -1;

    function fechar() { lista.classList.add('d-none'); ativo = -1; }

    function marcar(i) {
        ativo = i;
        [...lista.children].forEach((el, n) => el.classList.toggle('active', n === i));
        lista.children[i]?.scrollIntoView({ block: 'nearest' });
    }

    function escolher(reg) {
        oculto.value = reg.id;
        campo.value = textoLocalidade(tipo, reg);
        fechar();
    }

    async function pesquisar() {
        try {
            const resp = await fetch(urlLocalidades(LOCALIDADE[tipo].handler) + '&termo=' + encodeURIComponent(campo.value.trim()));
            regs = await resp.json();
        } catch { regs = []; }

        lista.innerHTML = '';
        ativo = -1;
        if (!regs.length) {
            lista.innerHTML = '<div class="list-group-item text-muted small">Nenhum cadastro encontrado. Use o + para cadastrar.</div>';
        } else {
            regs.forEach(reg => {
                const item = document.createElement('div');
                item.className = 'list-group-item list-group-item-action';
                item.innerHTML = escaparHtml(reg.nome) + (reg.detalhe ? ' <span class="text-muted small">- ' + escaparHtml(reg.detalhe) + '</span>' : '');
                // mousedown (antes do blur do campo) para o clique nao se perder.
                item.addEventListener('mousedown', e => { e.preventDefault(); escolher(reg); });
                lista.appendChild(item);
            });
        }
        lista.classList.remove('d-none');
    }

    campo.addEventListener('focus', pesquisar);
    campo.addEventListener('input', () => {
        oculto.value = '';
        clearTimeout(timer);
        timer = setTimeout(pesquisar, 250);
    });
    campo.addEventListener('keydown', e => {
        const aberta = !lista.classList.contains('d-none') && regs.length;
        if (e.key === 'ArrowDown' && aberta) { e.preventDefault(); marcar(Math.min(ativo + 1, regs.length - 1)); }
        else if (e.key === 'ArrowUp' && aberta) { e.preventDefault(); marcar(Math.max(ativo - 1, 0)); }
        else if (e.key === 'Enter') {
            e.preventDefault();
            if (aberta) escolher(regs[ativo >= 0 ? ativo : 0]);
        }
        else if (e.key === 'Escape' && !lista.classList.contains('d-none')) {
            // Fecha so a lista, sem fechar o modal.
            e.preventDefault();
            e.stopPropagation();
            fechar();
        }
    });
    campo.addEventListener('blur', () => {
        // Trocar de janela (Alt+Tab) tambem dispara blur: ai nao mexe em nada.
        if (!document.hasFocus()) return;
        fechar();
        if (!oculto.value) campo.value = '';
    });

    campo.dataset.droplistPronta = '1';
    pesquisar();
}

document.addEventListener('focusin', e => {
    const campo = e.target.closest?.('[data-droplist]');
    if (campo && !campo.dataset.droplistPronta) iniciarDroplist(campo);
});

// ===== Pesquisas (o "+" de cada campo) =====
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
        selecionar: c => definirCidade(campoCidadeAtivo, c.id, textoLocalidade('cidade', c)),
        editar: c => abrirCadastroCidade(c),
        novo: () => abrirCadastroCidade(null)
    });
}

function abrirPesquisaEstado() {
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
        selecionar: e => { elLocalidade('cidEstado').value = e.id; elLocalidade('cidEstadoNome').value = textoLocalidade('estado', e); },
        editar: e => abrirCadastroEstado(e),
        novo: () => abrirCadastroEstado(null)
    });
}

function abrirPesquisaPais() {
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
        selecionar: p => { elLocalidade('estPais').value = p.id; elLocalidade('estPaisNome').value = textoLocalidade('pais', p); },
        editar: p => abrirCadastroPais(p),
        novo: () => abrirCadastroPais(null)
    });
}

// ===== Cadastros (reg = registro da pesquisa para editar, null para novo) =====
function abrirCadastroCidade(c) {
    elLocalidade('cidMsg').innerHTML = '';
    elLocalidade('cidTitulo').textContent = c ? 'Editar Cidade' : 'Cadastrar Cidade';
    elLocalidade('cidId').value = c ? c.id : 0;
    elLocalidade('cidNome').value = c ? c.nome : '';
    elLocalidade('cidEstado').value = c ? (c.idEstado || '') : '';
    elLocalidade('cidEstadoNome').value = c && c.estado ? c.estado + (c.detalhe ? ' / ' + c.detalhe : '') : '';
    abrirModalEmpilhado('modalCidadeRapido');
}

function abrirCadastroEstado(e) {
    elLocalidade('estMsg').innerHTML = '';
    elLocalidade('estTitulo').textContent = e ? 'Editar Estado' : 'Cadastrar Estado';
    elLocalidade('estId').value = e ? e.id : 0;
    elLocalidade('estNome').value = e ? e.nome : '';
    elLocalidade('estUF').value = e ? (e.detalhe || '') : '';
    elLocalidade('estPais').value = e ? (e.idPais || '') : '';
    elLocalidade('estPaisNome').value = e ? (e.pais || '') : '';
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

// Depois de gravar: edicao volta para a pesquisa com a lista atualizada;
// cadastro novo ja fica selecionado no campo de quem chamou e fecha a pesquisa.
function concluirCadastro(modal, chave, editando, selecionar) {
    bootstrap.Modal.getInstance(elLocalidade(modal)).hide();
    if (editando) { recarregarPesquisa(chave); return; }
    selecionar();
    fecharPesquisa(chave);
}

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
        // Cidade editada que esta no campo: atualiza o nome exibido.
        if (id && campoCidadeAtivo && elLocalidade(campoCidadeAtivo)?.value == id)
            elLocalidade(campoCidadeAtivo + 'Nome').value = data.nome + ' / ' + data.uf;
        concluirCadastro('modalCidadeRapido', 'cidade', id > 0,
            () => definirCidade(campoCidadeAtivo, data.id, data.nome + ' / ' + data.uf));
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
        if (id && elLocalidade('cidEstado').value == id) elLocalidade('cidEstadoNome').value = data.nome + ' / ' + data.uf;
        concluirCadastro('modalEstadoRapido', 'estado', id > 0, () => {
            elLocalidade('cidEstado').value = data.id;
            elLocalidade('cidEstadoNome').value = data.nome + ' / ' + data.uf;
        });
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
        if (id && elLocalidade('estPais').value == id) elLocalidade('estPaisNome').value = data.nome;
        concluirCadastro('modalPaisRapido', 'pais', id > 0, () => {
            elLocalidade('estPais').value = data.id;
            elLocalidade('estPaisNome').value = data.nome;
        });
    } catch { msg.innerHTML = alertaErro('Erro ao salvar o país. Tente novamente.'); }
    finally { btn.disabled = false; }
}
