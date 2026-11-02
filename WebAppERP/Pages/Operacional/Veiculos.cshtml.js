// Script da tela Veículos (Pages/Operacional/Veiculos.cshtml).

// Filtro rapido da lista (funcao em wwwroot/js/module.js).
filtrarTabela('#tblVeiculos tbody tr', { texto: 'searchInput' });

// Mascara placa: antiga LLLNNNN (ex.: ABC1234) ou Mercosul LLLNLNN (ex.: ABC1D23)
function mascaraPlaca(v) {
    v = v.toUpperCase().replace(/[^A-Z0-9]/g, '').slice(0, 7);
    let r = '';
    for (let i = 0; i < v.length; i++) {
        const c = v[i];
        // posicoes 0,1,2 = letras; 4 = letra ou numero; 3,5,6 = numeros
        if (i < 3) r += c.replace(/[^A-Z]/g, '');
        else if (i === 4) r += c;
        else r += c.replace(/[^0-9]/g, '');
    }
    return r;
}
const elPlaca = document.getElementById('formDsPlaca');
elPlaca?.addEventListener('input', () => { elPlaca.value = mascaraPlaca(elPlaca.value); });

function limparForm() {
    document.getElementById('modalTitle').innerHTML = '<i class="bi bi-truck-flatbed me-2"></i>Novo Veículo';
    document.getElementById('formIdVeiculo').value = '0';
    document.getElementById('formDsPlaca').value = '';
    document.getElementById('formNrANTT').value = '';
    document.getElementById('formIdEstado').value = '';
}

function editarVeiculo(id, placa, idEstado, antt) {
    document.getElementById('modalTitle').innerHTML = '<i class="bi bi-pencil me-2"></i>Editar Veículo';
    document.getElementById('formIdVeiculo').value = id;
    document.getElementById('formDsPlaca').value = placa;
    document.getElementById('formIdEstado').value = idEstado;
    document.getElementById('formNrANTT').value = antt;
}

