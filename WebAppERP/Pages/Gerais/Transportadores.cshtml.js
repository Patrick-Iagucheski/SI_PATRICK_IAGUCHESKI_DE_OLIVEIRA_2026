// Script da tela Transportadores (Pages/Gerais/Transportadores.cshtml).

// Filtro da lista (funcao em wwwroot/js/module.js): filtra no botao Buscar ou no Enter.
const aplicarBusca = filtrarTabela('#tblTransportadores tbody tr', { texto: 'buscaTexto', codigo: 'buscaId', contador: 'contadorRegistros' });

// Modal de cadastro: componente CadastroTransportador (mascara, limpar e editar).
