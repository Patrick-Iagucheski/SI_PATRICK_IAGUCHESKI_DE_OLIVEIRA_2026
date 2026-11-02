// Script da tela Serviços (Pages/Gerais/Servicos.cshtml).

// Filtro rapido da lista (funcao em wwwroot/js/module.js).
filtrarTabela('.servico-card', { texto: 'searchInput' });

function limparForm() {
    document.getElementById('modalTitle').innerHTML = '<i class="bi bi-card-checklist me-2"></i>Novo Serviço';
    document.getElementById('sId').value = '0';
    document.getElementById('sNome').value = '';
    document.getElementById('sDesc').value = '';
    document.getElementById('sValor').value = '';
    document.getElementById('sDuracao').value = '';
    document.getElementById('sCat').value = '';
    document.getElementById('sAtivo').value = 'true';
}

function editarServico(id, nome, desc, valor, duracao, cat, ativo) {
    document.getElementById('modalTitle').innerHTML = '<i class="bi bi-pencil me-2"></i>Editar Serviço';
    document.getElementById('sId').value = id;
    document.getElementById('sNome').value = nome;
    document.getElementById('sDesc').value = desc;
    document.getElementById('sValor').value = valor;
    document.getElementById('sDuracao').value = duracao;
    document.getElementById('sCat').value = cat;
    document.getElementById('sAtivo').value = ativo === 'True' ? 'true' : 'false';
}

