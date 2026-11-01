// Script do componente CampoCidade (Pages/Shared/Components/CampoCidade/Default.cshtml).
// Incluido pelo proprio componente, uma vez por pagina. Cuida de:
//  - droplist dos campos com data-droplist="cidade|estado|pais";
//  - cadastro rapido de cidade / estado / pais (com busca no topo);
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
function tokenLocalidade() {
    return document.getElementById('localidadeModais').dataset.token;
}
function alertaLocalidade(m) {
    return '<div class="alert alert-danger py-1 px-2 small mb-2">' + m + '</div>';
}
function escaparHtmlLocalidade(s) {
    return String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}
async function postJsonLocalidade(url, body) {
    const resp = await fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': tokenLocalidade() },
        body: JSON.stringify(body)
    });
    return await resp.json();
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

// Campo de cidade que abriu o cadastro rapido (recebe a cidade gravada).
let campoCidadeAtivo = null;
function abrirCadastroCidade(id) {
    campoCidadeAtivo = id;
    abrirModalEmpilhado('modalCidadeRapido');
}

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
                item.innerHTML = escaparHtmlLocalidade(reg.nome) + (reg.detalhe ? ' <span class="text-muted small">- ' + escaparHtmlLocalidade(reg.detalhe) + '</span>' : '');
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

// ===== Busca no topo dos cadastros rapidos =====
// Antes de cadastrar, o usuario pesquisa se a cidade/estado/pais ja existe.
// Achou: Selecionar preenche o campo de origem e fecha o cadastro.
[
    { tipo: 'cidade', modal: 'modalCidadeRapido', campo: 'cidBusca',  lista: 'cidBuscaLista',
      aplicar: reg => definirCidade(campoCidadeAtivo, reg.id, textoLocalidade('cidade', reg)) },
    { tipo: 'estado', modal: 'modalEstadoRapido', campo: 'estBusca',  lista: 'estBuscaLista',
      aplicar: reg => { document.getElementById('cidEstado').value = reg.id; document.getElementById('cidEstadoNome').value = textoLocalidade('estado', reg); } },
    { tipo: 'pais',   modal: 'modalPaisRapido',   campo: 'paisBusca', lista: 'paisBuscaLista',
      aplicar: reg => { document.getElementById('estPais').value = reg.id; document.getElementById('estPaisNome').value = textoLocalidade('pais', reg); } }
].forEach(b => {
    const modal = document.getElementById(b.modal);
    const campo = document.getElementById(b.campo);
    const lista = document.getElementById(b.lista);
    if (!modal || !campo || !lista) return;
    let timer = null;

    async function pesquisar() {
        const termo = campo.value.trim();
        if (!termo) { lista.classList.add('d-none'); lista.innerHTML = ''; return; }
        lista.classList.remove('d-none');
        lista.innerHTML = '<div class="list-group-item text-muted small">Buscando...</div>';
        try {
            const regs = await (await fetch(urlLocalidades(LOCALIDADE[b.tipo].handler) + '&termo=' + encodeURIComponent(termo))).json();
            if (!regs.length) {
                lista.innerHTML = '<div class="list-group-item text-muted small">Nenhum cadastro encontrado. Preencha abaixo para cadastrar.</div>';
                return;
            }
            lista.innerHTML = '';
            regs.forEach(reg => {
                const item = document.createElement('div');
                item.className = 'list-group-item d-flex justify-content-between align-items-center';
                item.innerHTML = '<span>' + escaparHtmlLocalidade(reg.nome) + (reg.detalhe ? ' - ' + escaparHtmlLocalidade(reg.detalhe) : '') + '</span>' +
                    '<button type="button" class="btn btn-sm btn-outline-primary">Selecionar</button>';
                item.querySelector('button').addEventListener('click', () => {
                    b.aplicar(reg);
                    bootstrap.Modal.getInstance(modal).hide();
                });
                lista.appendChild(item);
            });
        } catch {
            lista.innerHTML = '<div class="list-group-item text-danger small">Falha ao pesquisar</div>';
        }
    }

    campo.addEventListener('input', () => { clearTimeout(timer); timer = setTimeout(pesquisar, 300); });
    campo.addEventListener('keydown', e => {
        if (e.key === 'Enter') { e.preventDefault(); clearTimeout(timer); pesquisar(); }
    });

    // Cada abertura comeca com a busca limpa e com o cursor nela.
    modal.addEventListener('show.bs.modal', () => {
        campo.value = '';
        lista.classList.add('d-none');
        lista.innerHTML = '';
    });
    modal.addEventListener('shown.bs.modal', () => campo.focus());
});

// ===== Cadastro rapido de Cidade =====
async function salvarCidadeRapido() {
    const nome = document.getElementById('cidNome').value.trim();
    const idEstado = document.getElementById('cidEstado').value;
    const msg = document.getElementById('cidMsg');
    const btn = document.getElementById('btnSalvarCidadeRapido');
    msg.innerHTML = '';
    if (!nome) { msg.innerHTML = alertaLocalidade('Informe o nome da cidade.'); return; }
    if (!idEstado) { msg.innerHTML = alertaLocalidade('Selecione o estado.'); return; }
    btn.disabled = true;
    try {
        const data = await postJsonLocalidade(urlLocalidades('CidadeRapido'), { nmCidade: nome, idEstado: parseInt(idEstado) });
        if (!data.sucesso) { msg.innerHTML = alertaLocalidade(data.mensagem); return; }
        definirCidade(campoCidadeAtivo, data.id, data.nome + ' / ' + data.uf);
        document.getElementById('cidNome').value = '';
        document.getElementById('cidEstado').value = '';
        document.getElementById('cidEstadoNome').value = '';
        bootstrap.Modal.getInstance(document.getElementById('modalCidadeRapido')).hide();
    } catch (e) { msg.innerHTML = alertaLocalidade('Erro ao salvar a cidade. Tente novamente.'); }
    finally { btn.disabled = false; }
}

// ===== Cadastro rapido de Estado =====
async function salvarEstadoRapido() {
    const nome = document.getElementById('estNome').value.trim();
    const uf = document.getElementById('estUF').value.trim().toUpperCase();
    const idPais = document.getElementById('estPais').value;
    const msg = document.getElementById('estMsg');
    const btn = document.getElementById('btnSalvarEstadoRapido');
    msg.innerHTML = '';
    if (!nome) { msg.innerHTML = alertaLocalidade('Informe o nome do estado.'); return; }
    if (uf.length !== 2) { msg.innerHTML = alertaLocalidade('Informe a UF (2 letras).'); return; }
    if (!idPais) { msg.innerHTML = alertaLocalidade('Selecione o pais.'); return; }
    btn.disabled = true;
    try {
        const data = await postJsonLocalidade(urlLocalidades('EstadoRapido'), { sgUF: uf, nmEstado: nome, idPais: parseInt(idPais) });
        if (!data.sucesso) { msg.innerHTML = alertaLocalidade(data.mensagem); return; }
        document.getElementById('cidEstado').value = data.id;
        document.getElementById('cidEstadoNome').value = data.nome + ' / ' + data.uf;
        document.getElementById('estNome').value = '';
        document.getElementById('estUF').value = '';
        document.getElementById('estPais').value = '';
        document.getElementById('estPaisNome').value = '';
        bootstrap.Modal.getInstance(document.getElementById('modalEstadoRapido')).hide();
    } catch (e) { msg.innerHTML = alertaLocalidade('Erro ao salvar o estado. Tente novamente.'); }
    finally { btn.disabled = false; }
}

// ===== Cadastro rapido de Pais =====
async function salvarPaisRapido() {
    const nome = document.getElementById('paisNome').value.trim();
    const sigla = document.getElementById('paisSigla').value.trim().toUpperCase();
    const ddi = document.getElementById('paisDDI').value.trim();
    const moeda = document.getElementById('paisMoeda').value.trim();
    const msg = document.getElementById('paisMsg');
    const btn = document.getElementById('btnSalvarPaisRapido');
    msg.innerHTML = '';
    if (!nome) { msg.innerHTML = alertaLocalidade('Informe o nome do pais.'); return; }
    if (!sigla) { msg.innerHTML = alertaLocalidade('Informe a sigla do pais.'); return; }
    btn.disabled = true;
    try {
        const data = await postJsonLocalidade(urlLocalidades('PaisRapido'), { nmPais: nome, sgPais: sigla, nrDDI: ddi || null, dsMoeda: moeda || null });
        if (!data.sucesso) { msg.innerHTML = alertaLocalidade(data.mensagem); return; }
        document.getElementById('estPais').value = data.id;
        document.getElementById('estPaisNome').value = data.nome;
        document.getElementById('paisNome').value = '';
        document.getElementById('paisSigla').value = '';
        document.getElementById('paisDDI').value = '';
        document.getElementById('paisMoeda').value = '';
        bootstrap.Modal.getInstance(document.getElementById('modalPaisRapido')).hide();
    } catch (e) { msg.innerHTML = alertaLocalidade('Erro ao salvar o pais. Tente novamente.'); }
    finally { btn.disabled = false; }
}
