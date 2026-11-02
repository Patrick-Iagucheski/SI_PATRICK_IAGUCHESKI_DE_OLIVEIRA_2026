// Script da tela Agendamentos (Pages/Operacional/Agendamentos.cshtml).

// Filtro rapido da lista (funcao em wwwroot/js/module.js).
filtrarTabela('#tblAgendamentos tbody tr', { texto: 'searchInput' });

function limparForm() {
    document.getElementById('aId').value = '0';
}

