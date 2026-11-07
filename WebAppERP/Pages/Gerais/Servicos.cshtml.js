// Script da tela Servicos (Pages/Gerais/Servicos.cshtml).

// Filtro da lista (funcao em wwwroot/js/module.js): filtra no botao Buscar ou no Enter.
const aplicarBusca = filtrarTabela('#tblServicos tbody tr', {
    texto: 'buscaTexto', codigo: 'buscaId', inativos: 'buscaInativos', contador: 'contadorRegistros'
});

// Modal de cadastro: componente CadastroServico (limpar e editar).
