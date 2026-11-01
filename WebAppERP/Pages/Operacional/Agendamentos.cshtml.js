// Script da tela Agendamentos (Pages/Operacional/Agendamentos.cshtml).

document.getElementById('searchInput')?.addEventListener('input', function () {
    const filter = this.value.toLowerCase();
    document.querySelectorAll('#tblAgendamentos tbody tr').forEach(row => {
        row.style.display = row.textContent.toLowerCase().includes(filter) ? '' : 'none';
    });
});

function limparForm() {
    document.getElementById('aId').value = '0';
}

