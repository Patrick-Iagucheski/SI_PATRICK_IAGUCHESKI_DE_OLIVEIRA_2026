// Script da tela Formas de Pagamento (Pages/Financeiro/FormasPagamento.cshtml).

// Filtro da lista (funcao em wwwroot/js/module.js): filtra no botao Buscar ou no Enter.
const aplicarBusca = filtrarTabela('#tblFormas tbody tr', {
    texto: 'buscaTexto', codigo: 'buscaId', inativos: 'buscaInativos', contador: 'contadorRegistros'
});

// Modal de cadastro: componente CadastroFormaPagamento (limpar e editar).
