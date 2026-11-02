// Script da tela Agendamentos (Pages/Operacional/Agendamentos.cshtml).

// Filtro da lista (funcao em wwwroot/js/module.js): filtra no botao Buscar ou no Enter.
const aplicarBusca = filtrarTabela('#tblAgendamentos tbody tr', {
    texto: 'buscaTexto', atributos: { status: 'buscaSituacao' }, contador: 'contadorRegistros'
});

// A data busca a agenda do dia no servidor (o formulario leva tambem os outros
// filtros na URL); com a mesma data, so filtra a lista carregada.
const campoData = document.getElementById('buscaData');
const dataMudou = () => campoData.value !== campoData.dataset.carregada;

function buscarAgenda() {
    if (dataMudou()) document.getElementById('formFiltro').submit();
    else aplicarBusca();
}

document.getElementById('buscaTexto').addEventListener('keydown', e => {
    if (e.key === 'Enter' && dataMudou()) document.getElementById('formFiltro').submit();
});

// Ao voltar da troca de data, reaplica os filtros que vieram na URL.
const filtrosUrl = new URLSearchParams(location.search);
document.getElementById('buscaTexto').value = filtrosUrl.get('texto') ?? '';
document.getElementById('buscaSituacao').value = filtrosUrl.get('situacao') ?? '';
aplicarBusca();

function limparForm() {
    document.getElementById('aId').value = '0';
}

