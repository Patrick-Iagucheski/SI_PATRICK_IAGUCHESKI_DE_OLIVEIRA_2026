// Script da tela Formas de Pagamento (Pages/Financeiro/FormasPagamento.cshtml).

// Filtro rapido da lista por texto e situacao (funcao em wwwroot/js/module.js).
filtrarTabela('#tblFormas tbody tr', { texto: 'searchInput', atributos: { status: 'filterStatus' } });

function limparForm() {
    document.getElementById('modalTitle').innerHTML = '<i class="bi bi-credit-card me-2"></i>Nova Forma de Pagamento';
    document.getElementById('formIdForma').value = '0';
    document.getElementById('formDsForma').value = '';
    document.getElementById('formFlAtivo').value = 'true';
}

function editarForma(id, desc, ativo) {
    document.getElementById('modalTitle').innerHTML = '<i class="bi bi-pencil me-2"></i>Editar Forma de Pagamento';
    document.getElementById('formIdForma').value = id;
    document.getElementById('formDsForma').value = desc;
    document.getElementById('formFlAtivo').value = ativo === 'True' ? 'true' : 'false';
}

