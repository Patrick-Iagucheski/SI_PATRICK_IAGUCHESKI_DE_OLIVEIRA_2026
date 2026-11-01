// Script da tela Cidades (Pages/Gerais/Cidades.cshtml).

function aplicarBusca() {
    const id = document.getElementById('buscaId').value.trim();
    const texto = document.getElementById('searchInput').value.toLowerCase();
    const uf = document.getElementById('filterEstado').value;
    document.querySelectorAll('#tblCidades tbody tr').forEach(row => {
        if (row.querySelector('td[colspan]')) return;
        const rowId = row.querySelector('td')?.textContent.trim();
        const matchId = !id || rowId === id;
        const matchTexto = row.textContent.toLowerCase().includes(texto);
        const matchUf = !uf || row.getAttribute('data-uf') === uf;
        row.style.display = (matchId && matchTexto && matchUf) ? '' : 'none';
    });
}

document.getElementById('searchInput')?.addEventListener('input', aplicarBusca);
document.getElementById('buscaId')?.addEventListener('input', aplicarBusca);
document.getElementById('filterEstado')?.addEventListener('change', aplicarBusca);

function limparForm() {
    document.getElementById('modalTitle').innerHTML = '<i class="bi bi-building me-2"></i>Nova Cidade';
    document.getElementById('formCodigoDisplay').value = '';
    document.getElementById('formIdCidade').value = '0';
    document.getElementById('formNmCidade').value = '';
    document.getElementById('formIdEstado').value = '';
}

function editarCidade(id, nome, idEstado) {
    document.getElementById('modalTitle').innerHTML = '<i class="bi bi-pencil me-2"></i>Editar Cidade';
    document.getElementById('formCodigoDisplay').value = id;
    document.getElementById('formIdCidade').value = id;
    document.getElementById('formNmCidade').value = nome;
    document.getElementById('formIdEstado').value = idEstado;
}

