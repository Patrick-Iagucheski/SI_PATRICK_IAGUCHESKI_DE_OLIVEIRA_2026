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

// Caixa de erro pequena, usada dentro dos modais.
function alertaErro(msg) {
    return '<div class="alert alert-danger py-1 px-2 small mb-2">' + msg + '</div>';
}

// ============================================================
// Listagens
// ============================================================

// Filtro local das listagens: esconde as linhas que nao batem com os campos
// de filtro e ja liga os eventos dos campos. Devolve a funcao que aplica o
// filtro (para um botao "Buscar", por exemplo).
//   seletor: linhas a filtrar (ex.: '#tblPaises tbody tr' ou '.servico-card')
//   campos.texto: id do campo de texto (procura em qualquer parte da linha)
//   campos.codigo: id do campo de codigo (compara com a 1a coluna)
//   campos.atributos: { atributo: idDoCampo } compara com data-<atributo> da linha
// Linhas de aviso ("Nenhum registro...", com td[colspan]) ficam sempre visiveis.
function filtrarTabela(seletor, campos) {
    const campo = id => id ? document.getElementById(id) : null;
    const texto = campo(campos.texto);
    const codigo = campo(campos.codigo);
    const atributos = Object.entries(campos.atributos || {}).map(([attr, id]) => [attr, campo(id)]);

    function aplicar() {
        const t = (texto?.value || '').toLowerCase();
        const c = (codigo?.value || '').trim();
        document.querySelectorAll(seletor).forEach(linha => {
            if (linha.querySelector('td[colspan]')) return;
            const ok = (!t || linha.textContent.toLowerCase().includes(t))
                && (!c || linha.querySelector('td')?.textContent.trim() === c)
                && atributos.every(([attr, el]) => !el?.value || linha.getAttribute('data-' + attr) === el.value);
            linha.style.display = ok ? '' : 'none';
        });
    }

    texto?.addEventListener('input', aplicar);
    codigo?.addEventListener('input', aplicar);
    atributos.forEach(([, el]) => el?.addEventListener('change', aplicar));
    return aplicar;
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
