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
// tela recarrega e mostra a mensagem verde. A validacao da propria tela
// (listener no form que chama preventDefault) roda antes e pode barrar o envio.
document.addEventListener('submit', async e => {
    const form = e.target;
    if (!form.matches('form[data-modal-ajax]') || e.defaultPrevented) return;
    e.preventDefault();

    const botao = form.querySelector('button[type="submit"]');
    exibirErro(form, null);
    if (botao) botao.disabled = true;

    try {
        const resp = await fetch(form.action, {
            method: 'POST',
            body: new FormData(form),
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        });
        const data = await resp.json();
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
    const corpo = alvo.querySelector('.modal-body') || alvo;
    let aviso = corpo.querySelector(':scope > .aviso-erro');
    if (!aviso) {
        aviso = document.createElement('div');
        aviso.className = 'aviso-erro';
        corpo.prepend(aviso);
    }
    aviso.innerHTML = msg ? alertaErro(msg) : '';
    if (!msg) return;
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
