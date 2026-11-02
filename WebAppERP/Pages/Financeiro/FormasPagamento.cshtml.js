// Script da tela Formas de Pagamento (Pages/Financeiro/FormasPagamento.cshtml).

// Filtro da lista (funcao em wwwroot/js/module.js): filtra no botao Buscar ou no Enter.
const aplicarBusca = filtrarTabela('#tblFormas tbody tr', {
    texto: 'buscaTexto', codigo: 'buscaId', inativos: 'buscaInativos', contador: 'contadorRegistros'
});

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

