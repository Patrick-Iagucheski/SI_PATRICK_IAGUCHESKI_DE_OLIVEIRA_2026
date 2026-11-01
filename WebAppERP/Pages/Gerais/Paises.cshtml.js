// Script da tela Países (Pages/Gerais/Paises.cshtml).

function aplicarBusca() {
    const id = document.getElementById('buscaId').value.trim();
    const txt = document.getElementById('searchInput').value.toLowerCase();
    document.querySelectorAll('#tblPaises tbody tr').forEach(row => {
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
    document.getElementById('modalTitle').innerHTML = '<i class="bi bi-globe-americas me-2"></i>Novo Pais';
    document.getElementById('formCodigoDisplay').value = ''; 
    document.getElementById('formIdPais').value = '0';
    document.getElementById('formNmPais').value = '';
    document.getElementById('formSgPais').value = '';
    document.getElementById('formNrDDI').value = '';
    document.getElementById('formDsMoeda').value = '';
}

function editarPais(id, nome, sigla, ddi, moeda) {
    document.getElementById('modalTitle').innerHTML = '<i class="bi bi-pencil me-2"></i>Editar Pais';
    document.getElementById('formCodigoDisplay').value = id;
    document.getElementById('formIdPais').value = id;
    document.getElementById('formNmPais').value = nome;
    document.getElementById('formSgPais').value = sigla;
    document.getElementById('formNrDDI').value = ddi;
    document.getElementById('formDsMoeda').value = moeda;
}

