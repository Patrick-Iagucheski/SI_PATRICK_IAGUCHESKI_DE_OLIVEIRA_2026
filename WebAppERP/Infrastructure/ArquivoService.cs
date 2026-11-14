namespace WebAppERP.Infrastructure;

// Arquivos enviados pelas telas (imagem do produto, logo da empresa...).
// Ficam numa pasta unica, fora do site (appsettings "Arquivos:PastaUploads",
// ex.: C:\WebERP\uploads): o sistema publicado no IIS e o rodado pelo Visual
// Studio enxergam as mesmas imagens, e publicar o site nao apaga nenhuma. Sem
// a configuracao, usa wwwroot/uploads. O endereco gravado no banco e sempre
// "/uploads/...": o Program.cs serve essa pasta nesse endereco. Pasta fora do
// git: o conteudo e dado do cliente, nao codigo. Cada Business informa a sua
// subpasta ("uploads", "uploads/empresa") e o seu limite.
public class ArquivoService
{
    // Inicio do endereco dos arquivos ("/uploads/...").
    public const string Raiz = "uploads";

    private static readonly string[] ExtensoesImagem = [".jpg", ".jpeg", ".png", ".webp", ".gif"];

    // Pasta fisica que corresponde ao endereco "/uploads".
    public string PastaFisica { get; }

    public ArquivoService(IWebHostEnvironment env, IConfiguration config)
    {
        var configurada = config["Arquivos:PastaUploads"];
        PastaFisica = string.IsNullOrWhiteSpace(configurada) ? Path.Combine(env.WebRootPath, Raiz) : configurada;
        Directory.CreateDirectory(PastaFisica);
    }

    // "uploads/empresa" ou "/uploads/empresa/abc.png" -> caminho dentro da PastaFisica.
    private string Fisico(string endereco)
    {
        var resto = endereco.TrimStart('/');
        if (!resto.StartsWith(Raiz))
            throw new InvalidOperationException($"Endereço de arquivo fora de /{Raiz}: {endereco}");
        resto = resto[Raiz.Length..].TrimStart('/');
        return Path.Combine(PastaFisica, resto.Replace('/', Path.DirectorySeparatorChar));
    }

    // Confere formato e tamanho da imagem enviada (sem arquivo: nada a validar).
    // rotulo: como a tela chama o arquivo na mensagem ("A imagem", "A logo").
    public void ValidarImagem(IFormFile? arquivo, long tamanhoMax, string rotulo)
    {
        if (arquivo is not { Length: > 0 }) return;

        var ext = Path.GetExtension(arquivo.FileName).ToLowerInvariant();
        if (!ExtensoesImagem.Contains(ext))
            throw new ValidacaoException("Formato de imagem inválido. Use JPG, PNG, WEBP ou GIF.");
        if (arquivo.Length > tamanhoMax)
            throw new ValidacaoException($"{rotulo} deve ter no máximo {tamanhoMax / (1024 * 1024)} MB.");
    }

    // Grava o arquivo na {pasta} (dentro da PastaFisica) e devolve o caminho para o banco
    // ("/uploads/abc.png"). Nome novo a cada envio: o navegador nao mostra a
    // versao antiga do cache.
    public async Task<string> Gravar(IFormFile arquivo, string pasta)
    {
        var ext = Path.GetExtension(arquivo.FileName).ToLowerInvariant();
        var destino = Fisico(pasta);
        Directory.CreateDirectory(destino);
        var nomeArquivo = $"{Guid.NewGuid():N}{ext}";
        using (var stream = File.Create(Path.Combine(destino, nomeArquivo)))
            await arquivo.CopyToAsync(stream);

        return $"/{pasta}/{nomeArquivo}";
    }

    // Apaga o arquivo gravado por Gravar. So mexe em arquivos da propria pasta
    // (nunca em imagens do sistema, como a logo padrao em images/).
    public void Remover(string? caminho, string pasta)
    {
        if (string.IsNullOrEmpty(caminho) || !caminho.StartsWith($"/{pasta}/") || caminho.Contains("..")) return;
        var fisico = Fisico(caminho);
        if (File.Exists(fisico))
        {
            try { File.Delete(fisico); } catch { /* ignora falha de exclusao */ }
        }
    }
}
