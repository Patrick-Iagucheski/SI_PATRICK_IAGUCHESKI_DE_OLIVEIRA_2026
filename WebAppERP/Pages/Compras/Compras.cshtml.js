// Script da tela Compras (Pages/Compras/Compras.cshtml).

// Filtro rapido da lista por texto e situacao (funcao em wwwroot/js/module.js).
filtrarTabela('#tblCompras tbody tr', { texto: 'searchInput', atributos: { status: 'filterStatus' } });

