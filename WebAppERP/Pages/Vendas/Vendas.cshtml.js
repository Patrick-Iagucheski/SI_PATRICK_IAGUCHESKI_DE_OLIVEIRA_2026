// Script da tela Vendas (Pages/Vendas/Vendas.cshtml).

function aplicarFiltros() {
    const texto = document.getElementById('searchInput').value.toLowerCase();
    const status = document.getElementById('filterStatus').value;
    document.querySelectorAll('#tblVendas tbody tr').forEach(row => {
        if (!row.hasAttribute('data-status')) return;
        const matchTexto = row.textContent.toLowerCase().includes(texto);
        const matchStatus = !status || row.getAttribute('data-status') === status;
        row.style.display = (matchTexto && matchStatus) ? '' : 'none';
    });
}
document.getElementById('searchInput')?.addEventListener('input', aplicarFiltros);
document.getElementById('filterStatus')?.addEventListener('change', aplicarFiltros);

