// Script da tela Condicoes de Pagamento (Pages/Financeiro/CondicoesPagamento.cshtml).
// Os valores que vem do servidor estao no bloco JSON #dadosTela da pagina.
const DADOS_TELA = JSON.parse(document.getElementById('dadosTela').textContent);

const PARCELAS_MAP = DADOS_TELA.PARCELAS_MAP;

// ===== Busca de Condicoes de Pagamento (filtros na pagina -> atualiza a grid via AJAX) =====
let resultadosBusca = [];

// Enter em qualquer filtro dispara a busca.
['buscaId', 'buscaCondicao'].forEach(id => {
    document.getElementById(id)?.addEventListener('keydown', e => {
        if (e.key === 'Enter') { e.preventDefault(); buscarCondicoes(); }
    });
});

function num2(v) { return Number(v || 0).toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }); }
function aggForma(parcelas) {
    const nomes = [...new Set((parcelas || []).map(p => p.formaNome || '-'))];
    return nomes.length === 0 ? '-' : (nomes.length === 1 ? escaparHtml(nomes[0]) : 'Varias');
}
function maxPrazo(parcelas) {
    return (parcelas && parcelas.length) ? Math.max(...parcelas.map(p => p.nrDiasVencimento)) : 0;
}

function montarLinha(c, i) {
    const toggleHtml = c.ativo
        ? '<form method="post" action="?handler=AlterarSituacao" class="d-inline">' + campoToken() + '<input type="hidden" name="id" value="' + c.id + '"><button type="submit" class="btn btn-icon btn-outline-warning" title="Inativar"><i class="bi bi-pause-circle"></i></button></form>'
        : '<form method="post" action="?handler=AlterarSituacao" class="d-inline">' + campoToken() + '<input type="hidden" name="id" value="' + c.id + '"><button type="submit" class="btn btn-icon btn-outline-success" title="Ativar"><i class="bi bi-play-circle"></i></button></form>';
    return '<tr>' +
        '<td><strong>' + escaparHtml(c.nome || '') + '</strong></td>' +
        '<td>' + aggForma(c.parcelas) + '</td>' +
        '<td class="text-center">' + maxPrazo(c.parcelas) + ' dia(s)</td>' +
        '<td class="text-center">' + ((c.parcelas || []).length) + '</td>' +
        '<td class="text-end">' + num2(c.juro) + '</td>' +
        '<td class="text-end">' + num2(c.multa) + '</td>' +
        '<td class="text-end">' + num2(c.desconto) + '</td>' +
        '<td class="text-center">' + statusBadge(c.ativo) + '</td>' +
        '<td class="text-center" style="white-space: nowrap;">' +
            toggleHtml + ' ' +
            '<button class="btn btn-icon btn-outline-primary" title="Editar" onclick="abrirEdicao(' + i + ')"><i class="bi bi-pencil"></i></button> ' +
            '<form method="post" action="?handler=Excluir" class="d-inline" onsubmit="return confirm(\'Deseja realmente excluir esta condicao de pagamento?\')">' +
                campoToken() +
                '<input type="hidden" name="id" value="' + c.id + '">' +
                '<button type="submit" class="btn btn-icon btn-outline-danger" title="Excluir"><i class="bi bi-trash"></i></button>' +
            '</form>' +
        '</td>' +
    '</tr>';
}

async function buscarCondicoes() {
    const tbody = document.querySelector('#tblCondicoes tbody');
    const id = document.getElementById('buscaId').value.trim();
    const condicao = document.getElementById('buscaCondicao').value.trim();
    const inativos = document.getElementById('buscaInativos').checked;

    const params = new URLSearchParams();
    if (id) params.append('id', id);
    if (condicao) params.append('condicao', condicao);
    params.append('incluirInativos', inativos);

    tbody.innerHTML = '<tr><td colspan="9" class="text-center text-muted py-3">Buscando...</td></tr>';
    try {
        const resp = await fetch('?handler=BuscarCondicoes&' + params.toString());
        resultadosBusca = await resp.json();

        const contador = document.getElementById('contadorRegistros');
        if (contador) contador.textContent = resultadosBusca.length + ' registros encontrados';

        if (!resultadosBusca.length) {
            tbody.innerHTML = '<tr><td colspan="9" class="text-center text-muted py-4">Nenhuma condição de pagamento encontrada</td></tr>';
            return;
        }
        tbody.innerHTML = resultadosBusca.map((c, i) => montarLinha(c, i)).join('');
    } catch (e) {
        tbody.innerHTML = '<tr><td colspan="9" class="text-center text-danger py-4">Erro ao buscar. Tente novamente.</td></tr>';
    }
}

function abrirEdicao(i) {
    mostrarRegistro('modalCondicao', resultadosBusca[i].registro);
    const c = resultadosBusca[i];
    editarCondicaoPorRegistro(c);
    bootstrap.Modal.getOrCreateInstance(document.getElementById('modalCondicao')).show();
}

