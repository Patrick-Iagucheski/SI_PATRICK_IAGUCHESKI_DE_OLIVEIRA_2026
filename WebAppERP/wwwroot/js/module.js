// Funcoes compartilhadas pelas telas e componentes: o equivalente, no front,
// do Module.cs do servidor (mesmas regras de CPF/CNPJ dos dois lados).
// Tudo que serviria a mais de uma tela fica aqui, em vez de ser copiado em
// cada .cshtml.js. Carregado no <head> do _Layout: os scripts dos componentes
// rodam no meio do <body> e ja precisam destas funcoes no carregamento.
// So declara funcoes: nao mexe na pagina nem depende de jQuery/Bootstrap.

// ============================================================
// Mascaras de campo (o banco guarda so os digitos)
// ============================================================

function mascaraCPF(v) {
    return v.replace(/\D/g, '').slice(0, 11)
        .replace(/^(\d{3})(\d)/, '$1.$2')
        .replace(/^(\d{3})\.(\d{3})(\d)/, '$1.$2.$3')
        .replace(/\.(\d{3})(\d)/, '.$1-$2');
}

function mascaraCNPJ(v) {
    return v.replace(/\D/g, '').slice(0, 14)
        .replace(/^(\d{2})(\d)/, '$1.$2')
        .replace(/^(\d{2})\.(\d{3})(\d)/, '$1.$2.$3')
        .replace(/\.(\d{3})(\d)/, '.$1/$2')
        .replace(/(\d{4})(\d)/, '$1-$2');
}

// Campo que aceita CPF ou CNPJ: ate 11 digitos formata como CPF, depois como CNPJ.
function mascaraCpfCnpj(v) {
    return v.replace(/\D/g, '').length <= 11 ? mascaraCPF(v) : mascaraCNPJ(v);
}

// Fixo (10 digitos) ou celular (11 digitos).
function mascaraTelefone(v) {
    v = v.replace(/\D/g, '').slice(0, 11);
    if (v.length > 10)
        return v.replace(/^(\d{2})(\d{5})(\d{4}).*/, '($1) $2-$3');
    return v.replace(/^(\d{2})(\d{4})(\d{0,4}).*/, '($1) $2-$3').trim();
}

function mascaraCEP(v) {
    return v.replace(/\D/g, '').slice(0, 8).replace(/^(\d{5})(\d)/, '$1-$2');
}

function mascaraRG(v) {
    return v.replace(/\D/g, '').slice(0, 9)
        .replace(/^(\d{2})(\d)/, '$1.$2')
        .replace(/^(\d{2})\.(\d{3})(\d)/, '$1.$2.$3')
        .replace(/\.(\d{3})(\d)/, '.$1-$2');
}

// Liga a mascara ao campo: formata a cada tecla digitada.
function aplicarMascara(id, fn) {
    const el = document.getElementById(id);
    el?.addEventListener('input', () => { el.value = fn(el.value); });
}

// ============================================================
// Validacoes (mesma conta do Module.cs no servidor)
// ============================================================

// CPF pelos digitos verificadores (aceita com ou sem mascara).
function cpfValido(cpf) {
    const d = (cpf || '').replace(/\D/g, '');
    if (d.length !== 11 || /^(\d)\1{10}$/.test(d)) return false;
    const calc = (qtd) => {
        let s = 0;
        for (let i = 0; i < qtd; i++) s += parseInt(d[i]) * (qtd + 1 - i);
        const r = s % 11;
        return r < 2 ? 0 : 11 - r;
    };
    return calc(9) === parseInt(d[9]) && calc(10) === parseInt(d[10]);
}

// CNPJ pelos digitos verificadores (aceita com ou sem mascara).
function cnpjValido(cnpj) {
    const d = (cnpj || '').replace(/\D/g, '');
    if (d.length !== 14 || /^(\d)\1{13}$/.test(d)) return false;
    const calc = (qtd, pesos) => {
        let s = 0;
        for (let i = 0; i < qtd; i++) s += parseInt(d[i]) * pesos[i];
        const r = s % 11;
        return r < 2 ? 0 : 11 - r;
    };
    const p1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
    const p2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
    return calc(12, p1) === parseInt(d[12]) && calc(13, p2) === parseInt(d[13]);
}

// ============================================================
// Chamadas ao servidor (AJAX)
// ============================================================

// Token de seguranca (antiforgery) da pagina. O _Layout coloca uma vez no
// <head>; todo POST feito por JavaScript manda ele junto.
function tokenAntiforgery() {
    return document.querySelector('meta[name="token-antiforgery"]')?.content || '';
}

// Campo escondido com o token, para formularios montados em JavaScript
// (ex.: botoes Excluir/Ativar das grades preenchidas pela busca).
function campoToken() {
    return '<input type="hidden" name="__RequestVerificationToken" value="' + tokenAntiforgery() + '">';
}

// POST com corpo JSON e o token. Devolve a resposta ja convertida
// (padrao das telas: { sucesso: true, ... } ou { sucesso: false, mensagem }).
async function postJson(url, dados) {
    const resp = await fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': tokenAntiforgery() },
        body: JSON.stringify(dados)
    });
    return await resp.json();
}

// Caixa de erro dos modais (cadastro e cadastro rapido): mesma aparencia em todos.
function alertaErro(msg) {
    return '<div class="alert alert-danger py-2 mb-3"><i class="bi bi-exclamation-triangle me-1"></i>' + escaparHtml(msg) + '</div>';
}

// Salvar dos modais de cadastro (<form method="post" data-modal-ajax>): grava
// por AJAX em vez de recarregar a pagina. No erro, a mensagem aparece no topo
// do modal, que continua aberto com tudo o que foi digitado; no sucesso, a
// tela recarrega e mostra a mensagem verde. Componente aberto por outra tela
// (form com data-evento, ex.: "transportador:salvo"): no sucesso o modal fecha
// e o evento avisa quem abriu, com o registro gravado (detail), sem recarregar. A validacao da propria tela
// (listener no form que chama preventDefault) roda antes e pode barrar o envio.
document.addEventListener('submit', async e => {
    const form = e.target;
    if (!form.matches('form[data-modal-ajax]') || e.defaultPrevented) return;
    e.preventDefault();

    const botao = form.querySelector('button[type="submit"]');
    exibirErro(form, null);
    if (botao) botao.disabled = true;

    const evento = form.dataset.evento;
    try {
        const resp = await fetch(form.action, {
            method: 'POST',
            body: new FormData(form),
            headers: evento ? { 'X-Requested-With': 'XMLHttpRequest' }
                            : { 'X-Requested-With': 'XMLHttpRequest', 'X-Recarregar': '1' }
        });
        const data = await resp.json();
        if (data.sucesso && evento) {
            bootstrap.Modal.getInstance(form.closest('.modal'))?.hide();
            document.dispatchEvent(new CustomEvent(evento, { detail: data }));
            return;
        }
        if (data.sucesso) { location.reload(); return; }
        exibirErro(form, data.mensagem);
    } catch {
        exibirErro(form, 'Não foi possível salvar. Tente novamente.');
    } finally {
        if (botao) botao.disabled = false;
    }
});

// Mostra (ou limpa, com msg null) o erro de validacao no topo da area onde o
// usuario precisa corrigir: o modal do formulario ou a secao (card) de uma tela
// de edicao, como Compra e Venda. Usado por toda validacao da tela e pela
// resposta do servidor; nunca alert(). A area fica como esta, com o que foi
// digitado, e o aviso some quando o usuario volta a mexer nos campos dela.
//   alvo: o <form> do modal ou a area (ex.: secaoDoCampo('itQt'))
function exibirErro(alvo, msg) {
    mostrarAviso(alvo, msg ? alertaErro(msg) : '');
}

// Confirmacao verde no mesmo lugar do erro (ex.: exclusao feita na pesquisa).
function exibirSucesso(alvo, msg) {
    mostrarAviso(alvo, '<div class="alert alert-success py-2 mb-3"><i class="bi bi-check-circle me-1"></i>' + escaparHtml(msg) + '</div>');
}

function mostrarAviso(alvo, html) {
    const corpo = alvo.querySelector('.modal-body') || alvo;
    let aviso = corpo.querySelector(':scope > .aviso-erro');
    if (!aviso) {
        aviso = document.createElement('div');
        aviso.className = 'aviso-erro';
        corpo.prepend(aviso);
    }
    aviso.innerHTML = html;
    if (!html) return;
    aviso.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
    const limpar = () => { aviso.innerHTML = ''; };
    corpo.addEventListener('input', limpar, { once: true });
    corpo.addEventListener('change', limpar, { once: true });
}

// Secao (card) da tela onde esta o campo, para exibirErro mostrar a mensagem
// perto dele. Ex.: exibirErro(secaoDoCampo('itQt'), 'Informe a quantidade.')
function secaoDoCampo(id) {
    const campo = document.getElementById(id);
    return campo.closest('.card-body') || campo.form || document.body;
}

// ============================================================
// Pesquisa em modal (componentes de cadastro)
// ============================================================
// Cada pesquisa tem o seu modal (um por chave: fornecedor, cidade, estado,
// pais...), montado por este codigo. Assim uma pesquisa abre por cima da
// outra (Cidade > Estado > Pais) sem misturar. A pesquisa declara:
//   chave, titulo, url() (handler que devolve JSON), filtros
//   [{ param, rotulo, tipo, col }], colunas, celulas(reg) (HTML ja escapado
//   de cada coluna), selecionar(reg) (preenche o campo de quem chamou) e, se
//   houver cadastro, editar(reg), novo() e excluir: { url(), nome(reg),
//   depois(reg) } (lixeira: usa o Excluir da tela do cadastro, com as regras do
//   Business; depois limpa o campo de quem chamou se o registro estava nele).
//   inativos: texto do checkbox "Exibir ... Inativos" (cadastros com ativo /
//   inativo). Marcado, envia incluirInativos=true e lista so os inativos, como
//   nas telas de cadastro; inativo pode ser editado (para reativar), mas nao
//   selecionado.
// Busca no botao Buscar ou no Enter, como nas telas de cadastro, e ao abrir
// ja lista os registros disponiveis.
const pesquisas = {};

function abrirPesquisa(cfg) {
    const modal = document.getElementById('pesquisa-' + cfg.chave) || criarModalPesquisa(cfg.chave);
    pesquisas[cfg.chave] = cfg;

    modal.querySelector('.modal-title').textContent = cfg.titulo;
    const filtros = modal.querySelector('[data-filtros]');
    filtros.innerHTML = cfg.filtros.map(f =>
        '<div class="col-md-' + f.col + '">' +
            '<label class="form-label small mb-1">' + f.rotulo + '</label>' +
            '<input type="' + (f.tipo || 'text') + '" class="form-control" data-param="' + f.param + '" autocomplete="off"' +
            (f.tipo === 'number' ? ' min="1"' : '') + ' />' +
        '</div>').join('');
    filtros.querySelectorAll('input').forEach(i => i.addEventListener('keydown', e => {
        if (e.key === 'Enter') { e.preventDefault(); recarregarPesquisa(cfg.chave); }
    }));
    modal.querySelector('[data-cabecalho]').innerHTML =
        cfg.colunas.map((c, i) => '<th' + (i === 0 ? ' style="width: 90px;"' : '') + '>' + c + '</th>').join('') +
        '<th class="text-center" style="width: 150px;">Ações</th>';
    modal.querySelector('[data-novo]').classList.toggle('d-none', !cfg.novo);
    const inativos = modal.querySelector('[data-inativos]');
    inativos.classList.toggle('d-none', !cfg.inativos);
    inativos.querySelector('input').checked = false;
    inativos.querySelector('label').textContent = cfg.inativos || '';

    bootstrap.Modal.getOrCreateInstance(modal).show();
    recarregarPesquisa(cfg.chave);
}

// Refaz a busca com os filtros atuais (ex.: depois de editar um registro).
async function recarregarPesquisa(chave) {
    const cfg = pesquisas[chave];
    const modal = document.getElementById('pesquisa-' + chave);
    if (!cfg || !modal) return;
    const corpo = modal.querySelector('tbody');
    const colspan = cfg.colunas.length + 1;

    const params = new URLSearchParams();
    modal.querySelectorAll('[data-param]').forEach(i => {
        if (i.type === 'checkbox') { if (i.checked) params.append(i.dataset.param, 'true'); }
        else if (i.value.trim()) params.append(i.dataset.param, i.value.trim());
    });

    exibirErro(modal, null);
    corpo.innerHTML = '<tr><td colspan="' + colspan + '" class="text-center text-muted py-3">Buscando...</td></tr>';
    try {
        const url = cfg.url();
        const lista = await (await fetch(url + (url.includes('?') ? '&' : '?') + params.toString())).json();
        if (!lista.length) {
            corpo.innerHTML = '<tr><td colspan="' + colspan + '" class="text-center text-muted py-3">Nenhum registro encontrado</td></tr>';
            return;
        }
        corpo.innerHTML = '';
        lista.forEach(reg => {
            const tr = document.createElement('tr');
            tr.innerHTML = cfg.celulas(reg).map(c => '<td>' + c + '</td>').join('') +
                '<td class="text-center text-nowrap">' +
                    (cfg.editar ? '<button type="button" class="btn btn-icon btn-outline-primary me-1" data-acao="editar" title="Editar"><i class="bi bi-pencil"></i></button>' : '') +
                    (cfg.excluir ? '<button type="button" class="btn btn-icon btn-outline-danger me-1" data-acao="excluir" title="Excluir"><i class="bi bi-trash"></i></button>' : '') +
                    (reg.ativo === false
                        ? '<span class="badge-status badge-inativo ms-1">Inativo</span>'
                        : '<button type="button" class="btn btn-icon btn-outline-success" data-acao="selecionar" title="Selecionar"><i class="bi bi-check-lg"></i></button>') +
                '</td>';
            tr.querySelector('[data-acao="selecionar"]')?.addEventListener('click', () => {
                cfg.selecionar(reg);
                fecharPesquisa(chave);
            });
            tr.querySelector('[data-acao="editar"]')?.addEventListener('click', () => cfg.editar(reg));
            tr.querySelector('[data-acao="excluir"]')?.addEventListener('click', () => excluirDaPesquisa(chave, reg));
            corpo.appendChild(tr);
        });
    } catch {
        corpo.innerHTML = '<tr><td colspan="' + colspan + '" class="text-center text-danger py-3">Falha ao pesquisar</td></tr>';
    }
}

// Lixeira da pesquisa: o mesmo Excluir da tela do cadastro (regras do
// Business, ex.: nao exclui pais com estado). A mensagem aparece no modal.
async function excluirDaPesquisa(chave, reg) {
    const cfg = pesquisas[chave];
    const modal = document.getElementById('pesquisa-' + chave);
    if (!confirm('Deseja realmente excluir "' + cfg.excluir.nome(reg) + '"?')) return;
    exibirErro(modal, null);
    try {
        const resp = await fetch(cfg.excluir.url(), {
            method: 'POST',
            body: new URLSearchParams({ id: reg.id, __RequestVerificationToken: tokenAntiforgery() }),
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        });
        const r = await resp.json();
        if (!r.sucesso) { exibirErro(modal, r.mensagem); return; }
        cfg.excluir.depois?.(reg);
        await recarregarPesquisa(chave);
        exibirSucesso(modal, r.mensagem || 'Registro excluído com sucesso.');
    } catch {
        exibirErro(modal, 'Não foi possível excluir. Tente novamente.');
    }
}

// Campo <select> que recebe o registro escolhido ou cadastrado na pesquisa:
// cria a opcao se ainda nao existe, atualiza o texto e seleciona.
function selecionarNoSelect(id, valor, texto) {
    const sel = document.getElementById(id);
    if (!sel) return;
    let opt = sel.querySelector('option[value="' + valor + '"]');
    if (!opt) {
        opt = document.createElement('option');
        opt.value = valor;
        sel.appendChild(opt);
    }
    opt.textContent = texto;
    sel.value = valor;
    sel.dispatchEvent(new Event('change', { bubbles: true }));
}

// Registro editado: so atualiza o texto da opcao (sem trocar a escolha).
function renomearNoSelect(id, valor, texto) {
    const sel = document.getElementById(id);
    const opt = sel?.querySelector('option[value="' + valor + '"]');
    if (!opt) return;
    opt.textContent = texto;
    // Campo de selecao com lupa: atualiza o texto exibido.
    if (sel.value == valor) sel.value = sel.value;
}

// Registro excluido: sai do <select> (e o campo fica vazio se era o escolhido).
function removerDoSelect(id, valor) {
    const sel = document.getElementById(id);
    const opt = sel?.querySelector('option[value="' + valor + '"]');
    if (!opt) return;
    if (sel.value == valor) {
        sel.value = '';
        sel.dispatchEvent(new Event('change', { bubbles: true }));
    }
    opt.remove();
}

function fecharPesquisa(chave) {
    const modal = document.getElementById('pesquisa-' + chave);
    if (modal) bootstrap.Modal.getOrCreateInstance(modal).hide();
}

// ===== Pesquisa ligada ao cadastro (componente em modo ajax) =====
// Cada componente de cadastro define a sua pesquisa uma vez (colunas, filtros,
// como abrir o formulario) e as telas so dizem o que fazer com o registro:
//   abrirPesquisaComCadastro(cfg, opcoes)
//   cfg: o mesmo do abrirPesquisa (chave, titulo, url, filtros, colunas, celulas)
//        + modal (id do modal de cadastro), evento ('x:salvo'), preencher(reg),
//        limpar(), urlExcluir() e nome(reg).
//   opcoes (da tela): selecionar(reg), atualizado(reg) e excluido(reg).
// Editar e salvar volta para a lista atualizada; Novo e salvar ja seleciona o
// registro no campo da tela e fecha a pesquisa. Sem o modal de cadastro na
// pagina, a pesquisa so seleciona.
const pesquisasComCadastro = {};

function estadoComCadastro(cfg) {
    let estado = pesquisasComCadastro[cfg.chave];
    if (!estado) {
        estado = pesquisasComCadastro[cfg.chave] = { emEdicao: false, opcoes: null };
        document.addEventListener(cfg.evento, e => {
            const reg = e.detail;
            // So responde quando o cadastro foi aberto por esta pesquisa.
            if (!document.getElementById('pesquisa-' + cfg.chave)?.classList.contains('show')) return;
            if (estado.emEdicao) {
                estado.emEdicao = false;
                estado.opcoes.atualizado?.(reg);
                recarregarPesquisa(cfg.chave);
                return;
            }
            // Novo gravado como inativo nao entra no campo (as listas so tem ativos).
            if (reg.ativo !== false) estado.opcoes.selecionar(reg);
            fecharPesquisa(cfg.chave);
        });
    }
    return estado;
}

function abrirPesquisaComCadastro(cfg, opcoes) {
    const estado = estadoComCadastro(cfg);
    estado.opcoes = opcoes;

    const temCadastro = !!document.getElementById(cfg.modal);
    abrirPesquisa({
        chave: cfg.chave,
        titulo: cfg.titulo,
        url: cfg.url,
        filtros: cfg.filtros,
        colunas: cfg.colunas,
        celulas: cfg.celulas,
        inativos: cfg.inativos,
        selecionar: opcoes.selecionar,
        editar: temCadastro ? reg => { estado.emEdicao = true; cfg.preencher(reg); abrirModalEmpilhado(cfg.modal); } : null,
        novo: temCadastro ? () => { estado.emEdicao = false; cfg.limpar(); abrirModalEmpilhado(cfg.modal); } : null,
        excluir: temCadastro ? { url: cfg.urlExcluir, nome: cfg.nome, depois: reg => opcoes.excluido?.(reg) } : null
    });
}

// ===== Campo de selecao (padrao dos campos ligados a um cadastro) =====
// Na tela fica um <select> normal (asp-for, opcoes, required) com
//   data-pesquisa="js que abre a pesquisa" (o botao + e o clique no campo).
// Ao carregar, o <select> fica escondido (continua sendo o valor do form e a
// lista de textos) e no lugar aparece um campo so de selecao com os botoes
// X (limpa a escolha; so aparece com valor) e + (abre a pesquisa, onde se
// seleciona ou cadastra pelo Novo).
// Nada e digitado: Enter/espaco abre a pesquisa, Delete/Backspace limpa.
// Quem mexe no valor pelo JS (sel.value = x, selecionarNoSelect...) continua
// funcionando: o texto exibido acompanha o <select>.
const valorDoSelect = Object.getOwnPropertyDescriptor(HTMLSelectElement.prototype, 'value');

function montarCampoSelecao(sel) {
    if (sel.dataset.campoMontado) return;
    sel.dataset.campoMontado = '1';

    const executar = js => (js ? new Function(js)() : null);
    const vazio = sel.querySelector('option[value=""]');

    const texto = document.createElement('input');
    texto.type = 'text';
    texto.id = sel.id + 'Nome';
    texto.className = 'form-control campo-selecao';
    texto.autocomplete = 'off';
    texto.placeholder = vazio ? vazio.textContent.trim() : 'Clique no + para pesquisar';
    texto.title = 'Adicionar / Editar / Pesquisar';
    // required fica no campo visivel: o navegador mostra o aviso nele.
    if (sel.required) { texto.required = true; sel.required = false; }

    const atualizarTexto = () => {
        const opt = sel.selectedOptions[0];
        texto.value = opt && opt.value !== '' ? opt.textContent.trim() : '';
        atualizarBotoes();
    };

    // sel.value = x pelo JS tambem atualiza o texto exibido.
    Object.defineProperty(sel, 'value', {
        configurable: true,
        get() { return valorDoSelect.get.call(this); },
        set(v) { valorDoSelect.set.call(this, v); atualizarTexto(); }
    });
    sel.addEventListener('change', atualizarTexto);

    const pesquisar = () => { if (!sel.disabled) executar(sel.dataset.pesquisa); };
    const limpar = () => {
        if (sel.disabled || sel.value === '') return;
        sel.value = '';
        sel.dispatchEvent(new Event('change', { bubbles: true }));
    };
    texto.addEventListener('click', pesquisar);
    texto.addEventListener('keydown', e => {
        if (e.key === 'Tab') return;
        if (!e.ctrlKey && !e.metaKey) e.preventDefault();
        if (e.key === 'Enter' || e.key === ' ') pesquisar();
        else if (e.key === 'Delete' || e.key === 'Backspace') limpar();
    });
    texto.addEventListener('paste', e => e.preventDefault());
    texto.addEventListener('drop', e => e.preventDefault());

    const grupo = document.createElement('div');
    grupo.className = 'input-group';
    sel.parentNode.insertBefore(grupo, sel);
    sel.classList.add('d-none');
    grupo.append(sel, texto);

    const botaoLimpar = document.createElement('button');
    botaoLimpar.type = 'button';
    botaoLimpar.className = 'btn btn-outline-secondary';
    botaoLimpar.title = 'Limpar';
    botaoLimpar.innerHTML = '<i class="bi bi-x-lg"></i>';
    botaoLimpar.addEventListener('click', limpar);

    const botaoPesquisar = document.createElement('button');
    botaoPesquisar.type = 'button';
    botaoPesquisar.className = 'btn btn-outline-primary';
    // A pesquisa seleciona, edita e cadastra (Novo): a dica diz isso.
    botaoPesquisar.title = 'Adicionar / Editar / Pesquisar';
    botaoPesquisar.innerHTML = '<i class="bi bi-plus-lg"></i>';
    botaoPesquisar.addEventListener('click', pesquisar);
    grupo.append(botaoLimpar, botaoPesquisar);

    // Campo desabilitado (somente leitura ou travado pelo JS da tela): o texto
    // fica desabilitado e os botoes somem; voltam quando o <select> e liberado.
    // O X so aparece com algo escolhido.
    function atualizarBotoes() {
        texto.disabled = sel.disabled;
        botaoPesquisar.classList.toggle('d-none', sel.disabled);
        botaoLimpar.classList.toggle('d-none', sel.disabled || valorDoSelect.get.call(sel) === '');
    }
    new MutationObserver(atualizarBotoes).observe(sel, { attributes: true, attributeFilter: ['disabled'] });

    atualizarTexto();
}

function montarCamposSelecao(raiz = document) {
    raiz.querySelectorAll('select[data-pesquisa]').forEach(montarCampoSelecao);
}
document.addEventListener('DOMContentLoaded', () => montarCamposSelecao());

// Opcoes prontas para quando o campo da tela e um <select>.
// texto(reg): o que aparece na opcao (padrao: reg.nome).
function opcoesDoSelect(idSelect, texto = r => r.nome, depoisDeSelecionar = null) {
    return {
        selecionar: r => { selecionarNoSelect(idSelect, r.id, texto(r)); depoisDeSelecionar?.(r); },
        atualizado: r => renomearNoSelect(idSelect, r.id, texto(r)),
        excluido: r => removerDoSelect(idSelect, r.id)
    };
}

// Igual ao opcoesDoSelect, mas tambem mantem a lista local da tela (ex.: a
// lista de produtos da Venda, com preco e saldo, ou as formas da Compra, para
// o nome na grade de parcelas).
//   paraLista(reg): o objeto guardado na lista (padrao: { id, nome })
function opcoesDoSelectComLista(idSelect, lista, paraLista = r => ({ id: r.id, nome: r.nome })) {
    const select = opcoesDoSelect(idSelect);
    const guardar = r => {
        const item = paraLista(r);
        const i = lista.findIndex(x => x.id === item.id);
        if (i >= 0) lista[i] = item; else lista.push(item);
    };
    return {
        selecionar: r => { guardar(r); select.selecionar(r); },
        atualizado: r => { guardar(r); select.atualizado(r); },
        excluido: r => {
            const i = lista.findIndex(x => x.id === r.id);
            if (i >= 0) lista.splice(i, 1);
            select.excluido(r);
        }
    };
}

// Cadastro rapido aberto pela pesquisa (cargo, marca, categoria, cidade...),
// depois de gravar: fecha o modal do cadastro; edicao volta para a pesquisa com
// a lista atualizada; cadastro novo chama selecionar() (coloca no campo de quem
// chamou) e fecha a pesquisa.
function concluirCadastro(idModal, chave, editando, selecionar) {
    bootstrap.Modal.getInstance(document.getElementById(idModal))?.hide();
    if (editando) { recarregarPesquisa(chave); return; }
    selecionar();
    fecharPesquisa(chave);
}

function criarModalPesquisa(chave) {
    const modal = document.createElement('div');
    modal.className = 'modal fade';
    modal.id = 'pesquisa-' + chave;
    modal.tabIndex = -1;
    modal.innerHTML =
        '<div class="modal-dialog modal-xl modal-dialog-scrollable"><div class="modal-content">' +
            '<div class="modal-header">' +
                '<h5 class="modal-title"></h5>' +
                '<button type="button" class="btn-close" data-bs-dismiss="modal"></button>' +
            '</div>' +
            '<div class="modal-body">' +
                '<div class="card mb-3"><div class="card-body py-3">' +
                    '<div class="row g-2 align-items-end" data-filtros></div>' +
                    '<div class="d-flex justify-content-between align-items-center gap-2 mt-3">' +
                        '<div class="form-check mb-0 d-none" data-inativos>' +
                            '<input class="form-check-input" type="checkbox" id="pesquisa-' + chave + '-inativos" data-param="incluirInativos" />' +
                            '<label class="form-check-label" for="pesquisa-' + chave + '-inativos"></label>' +
                        '</div>' +
                        '<div class="d-flex gap-2 ms-auto">' +
                            '<button type="button" class="btn btn-outline-primary d-none" data-novo><i class="bi bi-plus-lg me-1"></i>Novo</button>' +
                            '<button type="button" class="btn btn-primary" data-buscar><i class="bi bi-search me-1"></i>Buscar</button>' +
                        '</div>' +
                    '</div>' +
                '</div></div>' +
                '<div class="table-responsive busca-resultados"><table class="table">' +
                    '<thead><tr data-cabecalho></tr></thead><tbody></tbody>' +
                '</table></div>' +
            '</div>' +
        '</div></div>';
    modal.querySelector('[data-buscar]').addEventListener('click', () => recarregarPesquisa(chave));
    // Como nas telas: marcar ou desmarcar os inativos ja refaz a busca.
    modal.querySelector('[data-inativos] input').addEventListener('change', () => recarregarPesquisa(chave));
    modal.querySelector('[data-novo]').addEventListener('click', () => pesquisas[chave].novo());
    document.body.appendChild(modal);
    return modal;
}

// Modais empilhados (pesquisa > cadastro > outra pesquisa...): cada modal que
// abre fica acima dos que ja estao abertos, com o fundo escuro entre eles, e
// fechar o de cima mantem a tela travada enquanto houver modal aberto.
document.addEventListener('show.bs.modal', e => {
    const abertos = document.querySelectorAll('.modal.show').length;
    e.target.style.zIndex = 1055 + abertos * 20;
    setTimeout(() => {
        const fundos = document.querySelectorAll('.modal-backdrop');
        const fundo = fundos[fundos.length - 1];
        if (fundo) fundo.style.zIndex = 1050 + abertos * 20;
    });
});
document.addEventListener('hidden.bs.modal', () => {
    if (document.querySelector('.modal.show')) document.body.classList.add('modal-open');
});

// Ao abrir um modal, some o erro da tentativa anterior.
document.addEventListener('show.bs.modal', e => {
    e.target.querySelectorAll('.aviso-erro').forEach(a => a.innerHTML = '');
});

// ============================================================
// Listagens
// ============================================================

// Filtro local das listagens com poucos registros (ja carregados na pagina).
// Funciona como a busca das telas grandes (ex.: Clientes): so filtra no botao
// Buscar (que chama a funcao devolvida) ou no Enter dos campos; o checkbox de
// inativos filtra na hora. Atualiza o contador e mostra "Nenhum registro
// encontrado" quando o filtro esconde tudo.
//   seletor: linhas a filtrar (ex.: '#tblPaises tbody tr')
//   campos.texto: id do campo de texto (procura em qualquer parte da linha)
//   campos.codigo: id do campo de codigo (compara com data-id da linha ou, sem
//                  ele, com a 1a coluna)
//   campos.atributos: { atributo: idDoCampo } compara com data-<atributo> da linha
//   campos.inativos: id do checkbox "Exibir ... inativos" (desmarcado: so as
//                    linhas data-status="ativo"; marcado: so as "inativo")
//   campos.contador: id do texto "N registros encontrados"
// Linhas de aviso ("Nenhum registro...", com td[colspan]) nao entram no filtro.
function filtrarTabela(seletor, campos) {
    const campo = id => id ? document.getElementById(id) : null;
    const texto = campo(campos.texto);
    const codigo = campo(campos.codigo);
    const inativos = campo(campos.inativos);
    const contador = campo(campos.contador);
    const atributos = Object.entries(campos.atributos || {}).map(([attr, id]) => [attr, campo(id)]);
    const linhas = () => [...document.querySelectorAll(seletor)].filter(l => !l.querySelector('td[colspan]'));

    function aplicar() {
        const t = semAcento(texto?.value || '').trim();
        const c = (codigo?.value || '').trim();
        const status = inativos ? (inativos.checked ? 'inativo' : 'ativo') : null;
        const todas = linhas();
        let visiveis = 0;
        todas.forEach(linha => {
            const id = linha.dataset.id ?? linha.querySelector('td')?.textContent.trim();
            const ok = (!t || semAcento(linha.textContent).includes(t))
                && (!c || id === c)
                && (!status || linha.dataset.status === status)
                && atributos.every(([attr, el]) => !el?.value || linha.getAttribute('data-' + attr) === el.value);
            linha.style.display = ok ? '' : 'none';
            if (ok) visiveis++;
        });

        if (contador) contador.textContent = visiveis + ' registros encontrados';
        if (todas.length) avisoSemResultado(todas[0], visiveis === 0);
    }

    [texto, codigo].forEach(el => el?.addEventListener('keydown', e => {
        if (e.key === 'Enter') { e.preventDefault(); aplicar(); }
    }));
    inativos?.addEventListener('change', aplicar);
    if (inativos) aplicar();   // abre mostrando so os ativos, como nas telas grandes
    return aplicar;
}

// Texto em minusculas e sem acento, para comparar como as buscas do servidor
// ("parana" acha "Paraná").
function semAcento(s) {
    return String(s).normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase();
}

// Aviso "Nenhum registro encontrado" ao lado das linhas filtradas (linha de
// tabela ou card, conforme o tipo da primeira linha).
function avisoSemResultado(modelo, mostrar) {
    const pai = modelo.parentElement;
    let aviso = pai.querySelector(':scope > .aviso-sem-resultado');
    if (!aviso) {
        aviso = document.createElement(modelo.tagName === 'TR' ? 'tr' : 'div');
        aviso.className = 'aviso-sem-resultado' + (modelo.tagName === 'TR' ? '' : ' col-12');
        const msg = '<span class="text-muted">Nenhum registro encontrado</span>';
        aviso.innerHTML = modelo.tagName === 'TR'
            ? '<td colspan="' + modelo.children.length + '" class="text-center py-4">' + msg + '</td>'
            : '<div class="text-center py-4">' + msg + '</div>';
        pai.appendChild(aviso);
    }
    aviso.style.display = mostrar ? '' : 'none';
}

// ============================================================
// Formatacao
// ============================================================

// Texto seguro para montar HTML: o conteudo nao vira tag.
function escaparHtml(s) {
    return String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
}

// Data ISO (2026-11-01 ou 2026-11-01T00:00:00) no formato brasileiro (01/11/2026).
function dataBr(d, vazio = '-') {
    return d ? String(d).split('T')[0].split('-').reverse().join('/') : vazio;
}

// Valor com 2 casas no padrao brasileiro, sem o "R$" (ex.: 1.234,50).
// Vazio ou invalido vira 0,00.
function moeda(v) {
    return (Number(v) || 0).toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

// Selo Ativo/Inativo das listagens.
function statusBadge(ativo) {
    return ativo
        ? '<span class="badge-status badge-ativo">Ativo</span>'
        : '<span class="badge-status badge-inativo">Inativo</span>';
}
