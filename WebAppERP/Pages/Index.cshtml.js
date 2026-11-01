// Script da tela Principal (Pages/Index.cshtml).
// Os valores que vem do servidor estao no bloco JSON #dadosTela da pagina.
const DADOS_TELA = JSON.parse(document.getElementById('dadosTela').textContent);

// Menu de acesso rapido: lista em ordem alfabetica os itens do menu lateral
// (mesma fonte do menu, entao uma tela nova no menu aparece aqui sozinha)
(function () {
    const lista = document.getElementById('listaAcesso');
    const vazio = document.getElementById('acessoVazio');
    const busca = document.getElementById('buscaAcesso');
    const semAcento = t => t.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase();

    [...document.querySelectorAll('#sidebarNav a.sidebar-link:not([data-aba-principal])')]
        .map(a => ({ nome: a.textContent.trim(), href: a.getAttribute('href'), icone: a.querySelector('i')?.className || 'bi bi-window' }))
        .sort((x, y) => x.nome.localeCompare(y.nome, 'pt-BR'))
        .forEach(i => {
            const a = document.createElement('a');
            a.className = 'acesso-item';
            a.href = i.href;
            a.setAttribute('data-abrir-aba', '');
            a.dataset.abaIcone = i.icone;
            a.innerHTML = '<span><i></i><span></span></span><i class="bi bi-chevron-right"></i>';
            a.querySelector('span > i').className = i.icone + ' me-2';
            a.querySelector('span > span').textContent = i.nome;
            lista.appendChild(a);
        });

    busca.addEventListener('input', function () {
        const q = semAcento(this.value.trim());
        let visiveis = 0;
        lista.querySelectorAll('.acesso-item').forEach(a => {
            const ok = !q || semAcento(a.textContent).includes(q);
            a.classList.toggle('d-none', !ok);
            if (ok) visiveis++;
        });
        vazio.classList.toggle('d-none', visiveis > 0);
    });
})();


// Dados reais vindos do banco de dados
const faturamentoLabels = DADOS_TELA.faturamentoLabels;
const faturamentoValores = DADOS_TELA.faturamentoValores;
const servicosLabels = DADOS_TELA.servicosLabels;
const servicosValores = DADOS_TELA.servicosValores;

// Faturamento Semanal - Bar Chart
new Chart(document.getElementById('chartFaturamento'), {
    type: 'bar',
    data: {
        labels: faturamentoLabels,
        datasets: [{
            label: 'Faturamento (R$)',
            data: faturamentoValores,
            backgroundColor: 'rgba(59, 130, 246, 0.7)',
            borderColor: '#3b82f6',
            borderWidth: 1,
            borderRadius: 6
        }]
    },
    options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { display: false } },
        scales: {
            y: {
                beginAtZero: true,
                ticks: { callback: v => 'R$ ' + v }
            }
        }
    }
});

// Servicos Mais Solicitados - Doughnut Chart
const servicosCanvas = document.getElementById('chartServicos');
if (servicosValores.length > 0) {
    new Chart(servicosCanvas, {
        type: 'doughnut',
        data: {
            labels: servicosLabels,
            datasets: [{
                data: servicosValores,
                backgroundColor: ['#3b82f6', '#10b981', '#f59e0b', '#8b5cf6', '#ef4444'],
                borderWidth: 0
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: { padding: 12, font: { size: 11 } }
                }
            }
        }
    });
} else {
    servicosCanvas.parentElement.innerHTML =
        '<div class="d-flex align-items-center justify-content-center h-100 text-muted">Nenhum serviço registrado no período</div>';
}

