// Script da tela Configurações (Pages/Seguranca/Configuracoes.cshtml).
// Os valores que vem do servidor estao no bloco JSON #dadosTela da pagina.
const DADOS_TELA = JSON.parse(document.getElementById('dadosTela').textContent);

// Pre-visualizacao: nome e logo como vao ficar no menu lateral
(function () {
    const nome = document.getElementById('cfgNome');
    const arquivo = document.getElementById('cfgLogo');
    const logo = document.getElementById('cfgPreviewLogo');
    const nomePreview = document.getElementById('cfgPreviewNome');
    const LOGO_PADRAO = DADOS_TELA.LOGO_PADRAO;

    nome.addEventListener('input', () => nomePreview.textContent = nome.value.trim() || ' ');

    arquivo.addEventListener('change', () => {
        const f = arquivo.files && arquivo.files[0];
        if (f) logo.src = URL.createObjectURL(f);
    });

    // Limpa o arquivo escolhido e o caminho salvo: ao salvar, volta a logo padrao
    document.getElementById('cfgRemoverLogo').addEventListener('click', () => {
        arquivo.value = '';
        document.getElementById('cfgDsLogo').value = '';
        logo.src = LOGO_PADRAO;
    });
})();

