// Script do layout (Pages/Shared/_Layout.cshtml): menu lateral e abas internas.
// Funcoes compartilhadas pelas telas (mascaras, validacoes, moeda) ficam em module.js.

// ============================================
// Menu lateral
// ============================================
(function () {
    // Recolher / expandir secoes (abas)
    document.querySelectorAll('#sidebarNav .sidebar-group-header').forEach(function (btn) {
        btn.addEventListener('click', function () {
            btn.parentElement.classList.toggle('collapsed');
        });
    });

    // Busca no menu
    const menuSearch = document.getElementById('menuSearch');
    const menuEmpty = document.getElementById('menuSearchEmpty');
    menuSearch?.addEventListener('input', function () {
        const q = this.value.trim().toLowerCase();
        let totalVisivel = 0;

        document.querySelectorAll('#sidebarNav .sidebar-group').forEach(function (grupo) {
            let algum = false;
            grupo.querySelectorAll('.sidebar-item').forEach(function (item) {
                const txt = item.textContent.trim().toLowerCase();
                const ok = !q || txt.includes(q);
                item.style.display = ok ? '' : 'none';
                if (ok) { algum = true; totalVisivel++; }
            });
            // ao buscar, abre os grupos com resultado e esconde os vazios
            if (q) {
                grupo.style.display = algum ? '' : 'none';
                grupo.classList.toggle('collapsed', !algum);
            } else {
                grupo.style.display = '';
            }
        });

        // Dashboard (item solto)
        document.querySelectorAll('#sidebarNav > .sidebar-item').forEach(function (item) {
            const txt = item.textContent.trim().toLowerCase();
            const ok = !q || txt.includes(q);
            item.style.display = ok ? '' : 'none';
            if (ok) totalVisivel++;
        });

        if (menuEmpty) menuEmpty.style.display = (q && totalVisivel === 0) ? '' : 'none';
    });

    // Toggle da sidebar (botao do topo)
    document.getElementById('sidebarToggle')?.addEventListener('click', function () {
        document.getElementById('wrapper').classList.toggle('sidebar-collapsed');
    });

    // Abre um modal por cima de outro SEM usar data-bs-toggle. Isso e necessario
    // porque o handler nativo do data-bs-toggle="modal" do Bootstrap FECHA o modal
    // que ja estava aberto ("avoid conflict when clicking modal toggler while another
    // one is open"). Ao abrir via JS, o modal de baixo (ex.: Produto/Funcionario)
    // permanece aberto e preservado enquanto o cadastro rapido fica por cima.
    window.abrirModalEmpilhado = function (id) {
        var el = document.getElementById(id);
        if (el) bootstrap.Modal.getOrCreateInstance(el).show();
    };
})();

// ============================================
// Abas internas
// ============================================
// Na janela principal (html.com-abas) o layout funciona como "shell": cada item
// do menu abre a tela numa aba, e cada aba e um iframe com a propria pagina.
// Dentro do iframe (html.em-aba) o mesmo layout esconde menu e topo, entao
// links, POSTs, redirects e TempData continuam acontecendo dentro da aba.

// Links com data-abrir-aba (Menu da tela Principal, Atividades...) abrem numa
// aba nova do shell. Dentro de um iframe pede a janela principal; sem shell
// (pagina aberta direto) o link segue normalmente.
document.addEventListener('click', function (e) {
    var a = e.target.closest('a[data-abrir-aba]');
    if (!a || e.button !== 0 || e.ctrlKey || e.metaKey || e.shiftKey || e.altKey) return;
    var abrirAba = null;
    try { abrirAba = window.top.abrirAba; } catch (err) { /* outra origem */ }
    if (typeof abrirAba !== 'function') return;
    e.preventDefault();
    abrirAba(a.href, a.dataset.abaTitulo, a.dataset.abaIcone);
});

// Pagina que voltou do POST com a mensagem de sucesso do servidor (data-gravou,
// _Layout): gravacao confirmada, avisa as outras abas (avisarGravacao, module.js).
// As gravacoes por AJAX avisam no proprio module.js, so quando dao certo.
if (document.querySelector('[data-gravou]')) avisarGravacao();

(function () {
    var html = document.documentElement;
    if (!html.classList.contains('com-abas')) return;

    var barra = document.getElementById('abasBarra');
    var conteudo = document.getElementById('abasConteudo');
    var topoTitulo = document.getElementById('topbarTitulo');
    var linkPrincipal = document.querySelector('#sidebarNav [data-aba-principal]');
    if (!barra || !conteudo || !linkPrincipal) return;

    // Nome da empresa (Configuracoes): sufixo do titulo das paginas
    var nomeEmpresa = html.dataset.empresa || '';
    function sufixoTitulo() { return ' - ' + nomeEmpresa; }
    // v3: telas mudaram de pasta (por modulo); abas salvas com o
    // endereco antigo sao descartadas em vez de abrir pagina inexistente
    var CHAVE_STORAGE = 'webapperp.abas.v3';
    // chave = tela de origem (a mesma tela pode estar aberta em varias abas);
    // numero diferencia as de mesmo titulo: "Clientes", "Clientes (2)", ...
    // tituloFixo = titulo dado por quem abriu (ex.: "Venda #2"), nao vem da pagina
    var abas = [];      // { chave, url, titulo, tituloFixo, numero, icone, fixa, el, painel, versao }
    var ativa = null;

    // Cada gravacao em qualquer aba aumenta a versao dos dados. A aba guarda a
    // versao de quando a pagina dela carregou: se ficou para tras, a pagina e
    // recarregada ao voltar para ela (ex.: compra finalizada com a aba de
    // Contas a Pagar ja aberta mostrava os cartoes zerados).
    // origem: janela da aba que gravou; ela ja esta com os dados atuais.
    var versaoDados = 0;
    window.dadosAlterados = function (origem) {
        versaoDados++;
        abas.forEach(function (a) {
            if (a.painel && a.painel.contentWindow === origem) a.versao = versaoDados;
        });
    };

    // Recarrega so telas de consulta: tela de digitacao (Compra, Venda,
    // Configuracoes: <html data-manter-ao-voltar="1">) ou com modal aberto fica
    // como esta, para nao perder o que foi digitado.
    function atualizarSeDesatualizada(aba) {
        if (!aba.painel || aba.versao >= versaoDados) return;
        try {
            var doc = aba.painel.contentDocument;
            if (doc.documentElement.dataset.manterAoVoltar === '1') return;
            if (doc.querySelector('.modal.show')) return;
            aba.versao = versaoDados;
            // GET da pagina (sem handler): nao reenvia POST
            aba.painel.contentWindow.location.replace(aba.url);
        } catch (e) { /* pagina de outra origem */ }
    }

    // Chave da aba = caminho da URL normalizado (sem query e sem barra final)
    function chaveDe(url) {
        var u = new URL(url, location.href);
        return u.pathname.replace(/\/+$/, '').toLowerCase() || '/';
    }

    // URL relativa ao site, sem o handler do POST (?handler=Salvar etc.), para
    // que recarregar a aba volte ao GET da pagina
    function urlLimpa(url) {
        var u = new URL(url, location.href);
        u.searchParams.delete('handler');
        return u.pathname + u.search;
    }

    function linkDoMenu(chave) {
        return Array.from(document.querySelectorAll('#sidebarNav a.sidebar-link'))
            .find(function (a) { return chaveDe(a.href) === chave; }) || null;
    }

    function iconeDoLink(a) {
        var i = a && a.querySelector('i');
        return i ? i.className : 'bi bi-window';
    }

    // Menor numero livre entre as abas abertas com o mesmo titulo
    function proximoNumero(titulo) {
        var usados = abas.filter(function (a) { return !a.fixa && a.titulo === titulo; })
            .map(function (a) { return a.numero; });
        var n = 1;
        while (usados.indexOf(n) >= 0) n++;
        return n;
    }

    function rotulo(aba) {
        return aba.numero > 1 ? aba.titulo + ' (' + aba.numero + ')' : aba.titulo;
    }

    function atualizarRotulo(aba) {
        var r = rotulo(aba);
        aba.el.querySelector('.aba-titulo').textContent = r;
        aba.el.title = r;
        if (aba.painel) aba.painel.title = r;
    }

    function salvar() {
        try {
            var lista = abas.filter(function (a) { return !a.fixa; });
            sessionStorage.setItem(CHAVE_STORAGE, JSON.stringify({
                ativa: lista.indexOf(ativa),
                abas: lista.map(function (a) {
                    return {
                        chave: a.chave, url: a.url, titulo: a.titulo, tituloFixo: a.tituloFixo,
                        numero: a.numero, icone: a.icone
                    };
                })
            }));
        } catch (e) { /* sessionStorage indisponivel: abas nao sao lembradas */ }
    }

    function carregar() {
        try { return JSON.parse(sessionStorage.getItem(CHAVE_STORAGE)) || null; }
        catch (e) { return null; }
    }

    function criarAba(dados) {
        var aba = {
            chave: dados.chave, url: dados.url, titulo: dados.titulo, tituloFixo: !!dados.tituloFixo,
            numero: dados.fixa ? 1 : (dados.numero || proximoNumero(dados.titulo)),
            icone: dados.icone, fixa: !!dados.fixa, painel: null
        };

        var el = document.createElement('div');
        el.className = 'aba' + (aba.fixa ? ' aba-fixa' : '');
        el.setAttribute('role', 'tab');
        el.tabIndex = 0;
        el.innerHTML = '<i></i><span class="aba-titulo"></span>';
        el.querySelector('i').className = aba.icone;
        aba.el = el;
        atualizarRotulo(aba);

        if (!aba.fixa) {
            var x = document.createElement('button');
            x.type = 'button';
            x.className = 'aba-fechar';
            x.title = 'Fechar';
            x.innerHTML = '&times;';
            x.addEventListener('click', function (e) { e.stopPropagation(); fechar(aba); });
            el.appendChild(x);
            // botao do meio do mouse tambem fecha, como no navegador
            el.addEventListener('auxclick', function (e) { if (e.button === 1) fechar(aba); });
        }

        el.addEventListener('click', function () { ativar(aba); });
        el.addEventListener('keydown', function (e) {
            if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); ativar(aba); }
        });

        barra.appendChild(el);
        abas.push(aba);
        return aba;
    }

    // O iframe so e criado quando a aba e exibida pela primeira vez
    function garantirPainel(aba) {
        if (aba.painel) return;
        var f = document.createElement('iframe');
        f.className = 'aba-painel';
        f.title = rotulo(aba);
        f.src = aba.url;
        f.addEventListener('load', function () { aoCarregarPainel(aba); });
        conteudo.appendChild(f);
        aba.painel = f;
        aba.versao = versaoDados;
    }

    // Navegacao dentro da aba (link, POST, redirect): atualiza a URL da aba. O titulo
    // so vem da pagina quando a tela nao e um item do menu; senao fica o nome do menu.
    function aoCarregarPainel(aba) {
        // Pagina recem-carregada ja tem os dados atuais
        aba.versao = versaoDados;
        try {
            var doc = aba.painel.contentDocument;
            aba.url = urlLimpa(aba.painel.contentWindow.location.href);
            sincronizarEmpresa(doc);
            var t = (doc.title || '').replace(sufixoTitulo(), '').trim();
            if (t && !aba.fixa && !aba.tituloFixo && !linkDoMenu(aba.chave)) {
                aba.titulo = t;
                atualizarRotulo(aba);
            }
        } catch (e) { /* pagina de outra origem: mantem os dados anteriores */ }
        if (aba === ativa) refletirAtiva();
        salvar();
    }

    // Nome e logo podem mudar em Configuracoes: toda pagina carregada numa aba
    // ja vem com os dados atuais, e o menu da janela principal acompanha.
    function sincronizarEmpresa(doc) {
        var nome = doc.documentElement.dataset.empresa;
        var logoAba = doc.querySelector('#sidebar .sidebar-logo');
        var logo = document.querySelector('#sidebar .sidebar-logo');
        if (nome && nome !== nomeEmpresa) {
            nomeEmpresa = nome;
            html.dataset.empresa = nome;
            document.querySelector('#sidebar .sidebar-brand').textContent = nome;
            if (logo) logo.alt = nome;
        }
        if (logoAba && logo && logoAba.getAttribute('src') !== logo.getAttribute('src')) {
            logo.setAttribute('src', logoAba.getAttribute('src'));
            // icone da guia do navegador tambem e a logo
            document.getElementById('iconeEmpresa')?.setAttribute('href', logoAba.getAttribute('src'));
        }
    }

    // Topo, titulo da janela, URL e item do menu acompanham a aba ativa
    function refletirAtiva() {
        var titulo = ativa.fixa ? linkPrincipal.textContent.trim() : rotulo(ativa);
        if (topoTitulo) topoTitulo.textContent = titulo;
        document.title = titulo + sufixoTitulo();
        try { history.replaceState(null, '', ativa.url); } catch (e) { }

        var chaveUrl = chaveDe(ativa.url);
        document.querySelectorAll('#sidebarNav a.sidebar-link').forEach(function (a) {
            var c = chaveDe(a.href);
            a.classList.toggle('active', c === ativa.chave || c === chaveUrl);
        });
    }

    function ativar(aba) {
        if (aba !== ativa) atualizarSeDesatualizada(aba);
        garantirPainel(aba);
        abas.forEach(function (a) {
            var sim = a === aba;
            a.el.classList.toggle('ativa', sim);
            a.el.setAttribute('aria-selected', sim ? 'true' : 'false');
            if (a.painel) a.painel.classList.toggle('ativa', sim);
        });
        ativa = aba;
        aba.el.scrollIntoView({ block: 'nearest', inline: 'nearest' });
        refletirAtiva();
        salvar();
    }

    function fechar(aba) {
        var i = abas.indexOf(aba);
        if (i < 0 || aba.fixa) return;
        abas.splice(i, 1);
        aba.el.remove();
        if (aba.painel) aba.painel.remove();
        // fechou a ativa: vai para a vizinha da direita (ou a da esquerda)
        if (aba === ativa) ativar(abas[i] || abas[i - 1]);
        else salvar();
    }

    // Cada clique no menu abre a tela numa aba nova, mesmo que ela ja esteja aberta
    // (para alternar entre as abas abertas usa-se a barra de abas)
    function abrir(url, titulo, icone, tituloFixo) {
        var aba = criarAba({
            chave: chaveDe(url), url: urlLimpa(url), titulo: titulo, tituloFixo: tituloFixo, icone: icone
        });
        ativar(aba);
        return aba;
    }

    // Usado pelas paginas dentro das abas (links data-abrir-aba). Tela do menu
    // usa o nome e o icone do menu; as demais, o titulo informado pelo link.
    window.abrirAba = function (url, titulo, icone) {
        var link = linkDoMenu(chaveDe(url));
        if (link && link.hasAttribute('data-aba-principal')) { ativar(abas[0]); return; }
        if (link) abrir(url, link.textContent.trim(), iconeDoLink(link), false);
        else abrir(url, titulo || 'Nova aba', icone || 'bi bi-window', !!titulo);
    };

    // ---- Menu lateral: abre em aba (Ctrl/Shift/botao do meio mantem o padrao do navegador)
    document.getElementById('sidebarNav').addEventListener('click', function (e) {
        var a = e.target.closest('a.sidebar-link');
        if (!a || e.button !== 0 || e.ctrlKey || e.metaKey || e.shiftKey || e.altKey) return;
        e.preventDefault();
        a.blur();
        if (a.hasAttribute('data-aba-principal')) ativar(abas[0]);
        else abrir(a.href, a.textContent.trim(), iconeDoLink(a));

        // no celular o menu fica por cima do conteudo: fecha apos escolher
        if (window.innerWidth <= 768) document.getElementById('wrapper').classList.remove('sidebar-collapsed');
    });

    // ---- Inicializacao: aba fixa, abas lembradas e a aba da URL atual
    criarAba({
        chave: chaveDe(linkPrincipal.href),
        url: urlLimpa(linkPrincipal.href),
        titulo: 'Principal',
        icone: 'bi bi-house-door-fill',
        fixa: true
    });

    var salvo = carregar();
    var restauradas = [];
    if (salvo && Array.isArray(salvo.abas)) {
        salvo.abas.forEach(function (d) {
            if (!d || !d.chave || !d.url) return;
            var link = linkDoMenu(d.chave);
            restauradas.push(criarAba({
                chave: d.chave, url: d.url,
                titulo: link ? link.textContent.trim() : (d.titulo || d.chave),
                tituloFixo: d.tituloFixo,
                numero: d.numero,
                icone: d.icone || 'bi bi-window'
            }));
        });
    }

    // A URL da janela e a da aba ativa; ao abrir um endereco direto ele vira uma aba.
    // Com a mesma tela aberta em varias abas, prefere a que estava ativa.
    var chaveAtual = chaveDe(location.href);
    var urlAtual = urlLimpa(location.href);
    var eraAtiva = salvo ? restauradas[salvo.ativa] : null;
    var abaAtual = (eraAtiva && chaveDe(eraAtiva.url) === chaveAtual ? eraAtiva : null)
        || abas.find(function (a) { return chaveDe(a.url) === chaveAtual; })
        || abas.find(function (a) { return a.chave === chaveAtual; });
    if (abaAtual) {
        abaAtual.url = urlAtual;
        ativar(abaAtual);
    } else {
        var link = linkDoMenu(chaveAtual);
        var tituloAtual = link ? link.textContent.trim()
            : (document.title.replace(sufixoTitulo(), '').trim() || 'Nova aba');
        var nova = criarAba({ chave: chaveAtual, url: urlAtual, titulo: tituloAtual, icone: iconeDoLink(link) });
        ativar(nova);
    }
})();
