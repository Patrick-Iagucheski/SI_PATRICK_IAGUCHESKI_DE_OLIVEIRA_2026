// Script da tela Vendas (Pages/Vendas/Vendas.cshtml).

// Filtro da lista (funcao em wwwroot/js/module.js): filtra no botao Buscar ou no Enter.
const aplicarBusca = filtrarTabela('#tblVendas tbody tr', {
    texto: 'buscaTexto', codigo: 'buscaId', atributos: { status: 'buscaSituacao' }, contador: 'contadorRegistros'
});

