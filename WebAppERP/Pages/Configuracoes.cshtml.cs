using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;
using WebAppERP.Infrastructure;
using WebAppERP.Models;

namespace WebAppERP.Pages;

// Configuracoes do sistema: nome e logo da empresa exibidos no menu lateral.
public class ConfiguracoesModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly EmpresaService _empresa;

    private static readonly string[] ExtensoesPermitidas = [".jpg", ".jpeg", ".png", ".webp", ".gif"];
    private const long TamanhoMaxLogo = 5L * 1024 * 1024;
    private const string PastaLogo = "uploads/empresa";

    public ConfiguracoesModel(AppDbContext db, IWebHostEnvironment env, EmpresaService empresa)
    {
        _db = db;
        _env = env;
        _empresa = empresa;
    }

    [BindProperty]
    public string NmEmpresa { get; set; } = string.Empty;

    // Caminho da logo atual; vazio quando o usuario remove a logo (volta a padrao)
    [BindProperty]
    public string? DsLogo { get; set; }

    [BindProperty]
    public IFormFile? LogoUpload { get; set; }

    public bool TabelaCriada { get; set; } = true;

    public async Task OnGetAsync()
    {
        try
        {
            var empresa = await _db.Empresa.AsNoTracking().OrderBy(e => e.IdEmpresa).FirstOrDefaultAsync();
            NmEmpresa = empresa?.NmEmpresa ?? EmpresaService.NomePadrao;
            DsLogo = empresa?.DsLogo;
        }
        catch (Exception)
        {
            // Tabela ainda nao criada: a tela avisa para rodar o script.
            TabelaCriada = false;
            NmEmpresa = EmpresaService.NomePadrao;
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var nome = NmEmpresa?.Trim() ?? string.Empty;
        if (nome.Length == 0)
            return ComErro("Informe o nome da empresa.");
        if (nome.Length > 100)
            return ComErro("O nome da empresa deve ter no máximo 100 caracteres.");

        // ===== Upload da logo (opcional) =====
        string? novaLogo = null;
        if (LogoUpload is { Length: > 0 })
        {
            var ext = Path.GetExtension(LogoUpload.FileName).ToLowerInvariant();
            if (!ExtensoesPermitidas.Contains(ext))
                return ComErro("Formato de imagem inválido. Use JPG, PNG, WEBP ou GIF.");
            if (LogoUpload.Length > TamanhoMaxLogo)
                return ComErro("A logo deve ter no máximo 5 MB.");

            var pasta = Path.Combine(_env.WebRootPath, PastaLogo.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(pasta);
            // Nome novo a cada envio: o navegador nao mostra a logo antiga do cache.
            var nomeArquivo = $"{Guid.NewGuid():N}{ext}";
            using (var stream = System.IO.File.Create(Path.Combine(pasta, nomeArquivo)))
                await LogoUpload.CopyToAsync(stream);

            novaLogo = $"/{PastaLogo}/{nomeArquivo}";
        }

        var empresa = await _db.Empresa.OrderBy(e => e.IdEmpresa).FirstOrDefaultAsync();
        if (empresa == null)
        {
            empresa = new GerEmpresa { DtCriacao = DateTime.Now };
            _db.Empresa.Add(empresa);
        }
        else
        {
            empresa.DtEdicao = DateTime.Now;
        }

        empresa.NmEmpresa = nome;
        if (novaLogo != null)
        {
            // Enviou uma nova logo: apaga a antiga e usa a nova.
            RemoverLogoAntiga(empresa.DsLogo);
            empresa.DsLogo = novaLogo;
        }
        else if (string.IsNullOrEmpty(DsLogo))
        {
            // Usuario removeu a logo (campo oculto limpo): volta para a padrao.
            RemoverLogoAntiga(empresa.DsLogo);
            empresa.DsLogo = null;
        }

        await _db.SaveChangesAsync();
        _empresa.Limpar();

        TempData["Sucesso"] = "Configurações salvas com sucesso.";
        return RedirectToPage();
    }

    private IActionResult ComErro(string mensagem)
    {
        TempData["Erro"] = mensagem;
        return RedirectToPage();
    }

    // So apaga arquivos da pasta de logos enviadas (nunca a logo padrao em images/).
    private void RemoverLogoAntiga(string? caminhoRelativo)
    {
        if (string.IsNullOrEmpty(caminhoRelativo) || !caminhoRelativo.StartsWith($"/{PastaLogo}/")) return;
        var fisico = Path.Combine(_env.WebRootPath, caminhoRelativo.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (System.IO.File.Exists(fisico))
        {
            try { System.IO.File.Delete(fisico); } catch { /* ignora falha de exclusao */ }
        }
    }
}
