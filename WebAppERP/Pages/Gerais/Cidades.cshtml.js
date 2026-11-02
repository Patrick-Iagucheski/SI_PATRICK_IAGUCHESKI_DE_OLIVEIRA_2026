// Script da tela Cidades (Pages/Gerais/Cidades.cshtml).

// Filtro rapido por codigo, texto e UF (funcao em wwwroot/js/module.js); o botao Buscar chama aplicarBusca().
const aplicarBusca = filtrarTabela('#tblCidades tbody tr', { texto: 'searchInput', codigo: 'buscaId', atributos: { uf: 'filterEstado' } });

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

