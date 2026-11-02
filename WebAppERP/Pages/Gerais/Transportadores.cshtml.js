// Script da tela Transportadores (Pages/Gerais/Transportadores.cshtml).

// Filtro da lista (funcao em wwwroot/js/module.js): filtra no botao Buscar ou no Enter.
const aplicarBusca = filtrarTabela('#tblTransportadores tbody tr', { texto: 'buscaTexto', codigo: 'buscaId', contador: 'contadorRegistros' });

// Mascara dinamica CPF (<= 11 digitos) ou CNPJ (> 11 digitos)
function mascaraCpfCnpj(v) {
    v = v.replace(/\D/g, '').slice(0, 14);
    if (v.length <= 11) {
        return v.replace(/^(\d{3})(\d)/, '$1.$2')
                .replace(/^(\d{3})\.(\d{3})(\d)/, '$1.$2.$3')
                .replace(/\.(\d{3})(\d)/, '.$1-$2');
    }
    return v.replace(/^(\d{2})(\d)/, '$1.$2')
            .replace(/^(\d{2})\.(\d{3})(\d)/, '$1.$2.$3')
            .replace(/\.(\d{3})(\d)/, '.$1/$2')
            .replace(/(\d{4})(\d)/, '$1-$2');
}
const elDoc = document.getElementById('formNrCpfCnpj');
elDoc?.addEventListener('input', () => { elDoc.value = mascaraCpfCnpj(elDoc.value); });

function limparForm() {
    document.getElementById('modalTitle').innerHTML = '<i class="bi bi-truck-front me-2"></i>Novo Transportador';
    document.getElementById('formIdTransportador').value = '0';
    document.getElementById('formDsRazaoSocial').value = '';
    document.getElementById('formNrCpfCnpj').value = '';
    document.getElementById('formDsEndereco').value = '';
    limparCidade('formIdCidade');
    document.getElementById('formNrInscEstadual').value = '';
}

function editarTransportador(id, razao, doc, end, cidade, ie) {
    document.getElementById('modalTitle').innerHTML = '<i class="bi bi-pencil me-2"></i>Editar Transportador';
    document.getElementById('formIdTransportador').value = id;
    document.getElementById('formDsRazaoSocial').value = razao;
    document.getElementById('formNrCpfCnpj').value = doc;
    document.getElementById('formDsEndereco').value = end;
    definirCidade('formIdCidade', cidade);
    document.getElementById('formNrInscEstadual').value = ie;
}

