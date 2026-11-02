// Script da tela Países (Pages/Gerais/Paises.cshtml).

// Filtro rapido por codigo e texto (funcao em wwwroot/js/module.js); o botao Buscar chama aplicarBusca().
const aplicarBusca = filtrarTabela('#tblPaises tbody tr', { texto: 'searchInput', codigo: 'buscaId' });

function limparForm() {
    document.getElementById('modalTitle').innerHTML = '<i class="bi bi-globe-americas me-2"></i>Novo País';
    document.getElementById('formCodigoDisplay').value = ''; 
    document.getElementById('formIdPais').value = '0';
    document.getElementById('formNmPais').value = '';
    document.getElementById('formSgPais').value = '';
    document.getElementById('formNrDDI').value = '';
    document.getElementById('formDsMoeda').value = '';
}

function editarPais(id, nome, sigla, ddi, moeda) {
    document.getElementById('modalTitle').innerHTML = '<i class="bi bi-pencil me-2"></i>Editar País';
    document.getElementById('formCodigoDisplay').value = id;
    document.getElementById('formIdPais').value = id;
    document.getElementById('formNmPais').value = nome;
    document.getElementById('formSgPais').value = sigla;
    document.getElementById('formNrDDI').value = ddi;
    document.getElementById('formDsMoeda').value = moeda;
}

