using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;

namespace WebAppERP.Business.Seguranca;

// Regras de negocio da empresa (tbSegEmpresas, uma linha so): nome e logo
// exibidos no menu lateral, no titulo das paginas e no favicon.
// A leitura com cache para o layout fica no EmpresaService.
public class EmpresaBusiness
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    private static readonly string[] ExtensoesPermitidas = [".jpg", ".jpeg", ".png", ".webp", ".gif"];
    private const long TamanhoMaxLogo = 5L * 1024 * 1024; // 5 MB
    private const string PastaLogo = "uploads/empresa";

    public EmpresaBusiness(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    // ============================================================
    // Consultas
    // ============================================================

    // Dados atuais da empresa (null enquanto nada foi salvo).
    public async Task<Empresa?> GetEmpresa() =>
        await _db.Empresa.AsNoTracking().OrderBy(e => e.IdEmpresa).FirstOrDefaultAsync();

    // ============================================================
    // Gravacao
    // ============================================================

    // Grava o nome e a logo. logo: arquivo novo (opcional). removerLogo: o
    // usuario tirou a logo atual e quer voltar para a padrao.
    public async Task<Empresa> Salvar(string? nome, IFormFile? logo, bool removerLogo)
    {
        nome = nome?.Trim() ?? string.Empty;
        if (nome.Length == 0)
            throw new ValidacaoException("Informe o nome da empresa.");
        if (nome.Length > 100)
            throw new ValidacaoException("O nome da empresa deve ter no máximo 100 caracteres.");

        var temLogoNova = logo is { Length: > 0 };
        if (temLogoNova)
        {
            var ext = Path.GetExtension(logo!.FileName).ToLowerInvariant();
            if (!ExtensoesPermitidas.Contains(ext))
                throw new ValidacaoException("Formato de imagem inválido. Use JPG, PNG, WEBP ou GIF.");
            if (logo.Length > TamanhoMaxLogo)
                throw new ValidacaoException("A logo deve ter no máximo 5 MB.");
        }

        var empresa = await _db.Empresa.OrderBy(e => e.IdEmpresa).FirstOrDefaultAsync();
        if (empresa == null)
        {
            empresa = new Empresa { DtCriacao = DateTime.Now };
            _db.Empresa.Add(empresa);
        }
        else
        {
            empresa.DtEdicao = DateTime.Now;
        }

        empresa.NmEmpresa = nome;

        // A logo so vai para o disco depois de todas as validacoes.
        var logoAntiga = empresa.DsLogo;
        string? novaLogo = null;
        if (temLogoNova)
        {
            novaLogo = await GravarLogo(logo!);
            empresa.DsLogo = novaLogo;
        }
        else if (removerLogo)
        {
            empresa.DsLogo = null;
        }
        // Sem logo nova e sem remocao: mantem a atual.

        try
        {
            await _db.SaveChangesAsync();
        }
        catch
        {
            // Banco recusou: a logo nova nao fica perdida no disco.
            RemoverLogo(novaLogo);
            throw;
        }

        if (logoAntiga != empresa.DsLogo)
            RemoverLogo(logoAntiga);

        return empresa;
    }

    // ============================================================
    // Logo (wwwroot/uploads/empresa)
    // ============================================================

    private async Task<string> GravarLogo(IFormFile logo)
    {
        var ext = Path.GetExtension(logo.FileName).ToLowerInvariant();
        var pasta = Path.Combine(_env.WebRootPath, PastaLogo.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(pasta);
        // Nome novo a cada envio: o navegador nao mostra a logo antiga do cache.
        var nomeArquivo = $"{Guid.NewGuid():N}{ext}";
        using (var stream = File.Create(Path.Combine(pasta, nomeArquivo)))
            await logo.CopyToAsync(stream);

        return $"/{PastaLogo}/{nomeArquivo}";
    }

    // So apaga arquivos da pasta de logos enviadas (nunca a logo padrao em images/).
    private void RemoverLogo(string? caminhoRelativo)
    {
        if (string.IsNullOrEmpty(caminhoRelativo) || !caminhoRelativo.StartsWith($"/{PastaLogo}/")) return;
        var fisico = Path.Combine(_env.WebRootPath, caminhoRelativo.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(fisico))
        {
            try { File.Delete(fisico); } catch { /* ignora falha de exclusao */ }
        }
    }
}
