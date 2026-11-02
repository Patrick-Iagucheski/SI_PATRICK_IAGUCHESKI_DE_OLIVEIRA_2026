// Script da tela Compras (Pages/Compras/Compras.cshtml).

// Filtro da lista (funcao em wwwroot/js/module.js): filtra no botao Buscar ou no Enter.
const aplicarBusca = filtrarTabela('#tblCompras tbody tr', {
    texto: 'buscaTexto', codigo: 'buscaId', atributos: { status: 'buscaSituacao' }, contador: 'contadorRegistros'
});

