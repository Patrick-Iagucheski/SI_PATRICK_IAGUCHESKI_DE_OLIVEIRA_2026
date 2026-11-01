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
// Formatacao
// ============================================================

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
