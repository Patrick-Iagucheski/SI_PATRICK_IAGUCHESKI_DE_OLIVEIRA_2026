// Script da tela Estados (Pages/Gerais/Estados.cshtml).

function aplicarBusca() {
    const id = document.getElementById('buscaId').value.trim();
    const txt = document.getElementById('searchInput').value.toLowerCase();
    document.querySelectorAll('#tblEstados tbody tr').forEach(row => {
        if (row.querySelector('td[colspan]')) return;
        const rowId = row.querySelector('td')?.textContent.trim();
        const matchId = !id || rowId === id;
        const matchTxt = row.textContent.toLowerCase().includes(txt);
        row.style.display = (matchId && matchTxt) ? '' : 'none';
    });
}
document.getElementById('searchInput')?.addEventListener('input', aplicarBusca);
document.getElementById('buscaId')?.addEventListener('input', aplicarBusca);

function limparForm() {
    document.getElementById('modalTitle').innerHTML = '<i class="bi bi-map me-2"></i>Novo Estado';
    document.getElementById('formCodigoDisplay').value = '';
    document.getElementById('formIdEstado').value = '0';
    document.getElementById('formSgUF').value = '';
    document.getElementById('formNmEstado').value = '';
    document.getElementById('formIdPais').value = '';
}

function editarEstado(id, uf, nome, idPais) {
    document.getElementById('modalTitle').innerHTML = '<i class="bi bi-pencil me-2"></i>Editar Estado';
    document.getElementById('formCodigoDisplay').value = id;
    document.getElementById('formIdEstado').value = id;
    document.getElementById('formSgUF').value = uf;
    document.getElementById('formNmEstado').value = nome;
    document.getElementById('formIdPais').value = idPais;
}

