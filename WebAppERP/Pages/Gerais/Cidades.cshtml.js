// Script da tela Cidades (Pages/Gerais/Cidades.cshtml).

// Filtro da lista (funcao em wwwroot/js/module.js): filtra no botao Buscar ou no Enter.
const aplicarBusca = filtrarTabela('#tblCidades tbody tr', {
    texto: 'buscaTexto', codigo: 'buscaId', atributos: { uf: 'buscaEstado' }, contador: 'contadorRegistros'
});

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

