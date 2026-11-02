// Script da tela Estados (Pages/Gerais/Estados.cshtml).

// Filtro rapido por codigo e texto (funcao em wwwroot/js/module.js); o botao Buscar chama aplicarBusca().
const aplicarBusca = filtrarTabela('#tblEstados tbody tr', { texto: 'searchInput', codigo: 'buscaId' });

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

