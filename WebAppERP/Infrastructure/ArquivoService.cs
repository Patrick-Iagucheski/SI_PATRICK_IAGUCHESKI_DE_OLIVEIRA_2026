namespace WebAppERP.Infrastructure;

// Arquivos enviados pelas telas (imagem do produto, logo da empresa...),
// gravados em wwwroot/uploads. Pasta fora do git: o conteudo e dado do
// cliente, nao codigo. Cada Business informa a sua subpasta e o seu limite.
public class ArquivoService
{
    private static readonly string[] ExtensoesImagem = [".jpg", ".jpeg", ".png", ".webp", ".gif"];

    private readonly IWebHostEnvironment _env;

    public ArquivoService(IWebHostEnvironment env) => _env = env;

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

    // Grava o arquivo em wwwroot/{pasta} e devolve o caminho para o banco
    // ("/uploads/abc.png"). Nome novo a cada envio: o navegador nao mostra a
    // versao antiga do cache.
    public async Task<string> Gravar(IFormFile arquivo, string pasta)
    {
        var ext = Path.GetExtension(arquivo.FileName).ToLowerInvariant();
        var destino = Path.Combine(_env.WebRootPath, pasta.Replace('/', Path.DirectorySeparatorChar));
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
        var fisico = Path.Combine(_env.WebRootPath, caminho.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(fisico))
        {
            try { File.Delete(fisico); } catch { /* ignora falha de exclusao */ }
        }
    }
}
