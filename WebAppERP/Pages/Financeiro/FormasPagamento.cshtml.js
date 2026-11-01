// Script da tela Formas de Pagamento (Pages/Financeiro/FormasPagamento.cshtml).

function aplicarFiltros() {
    const texto = document.getElementById('searchInput').value.toLowerCase();
    const status = document.getElementById('filterStatus').value;
    document.querySelectorAll('#tblFormas tbody tr').forEach(row => {
        const matchTexto = row.textContent.toLowerCase().includes(texto);
        const matchStatus = !status || row.getAttribute('data-status') === status;
        row.style.display = (matchTexto && matchStatus) ? '' : 'none';
    });
}
document.getElementById('searchInput')?.addEventListener('input', aplicarFiltros);
document.getElementById('filterStatus')?.addEventListener('change', aplicarFiltros);

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

