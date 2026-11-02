// Script da tela Vendas (Pages/Vendas/Vendas.cshtml).

// Filtro rapido da lista por texto e situacao (funcao em wwwroot/js/module.js).
filtrarTabela('#tblVendas tbody tr', { texto: 'searchInput', atributos: { status: 'filterStatus' } });

