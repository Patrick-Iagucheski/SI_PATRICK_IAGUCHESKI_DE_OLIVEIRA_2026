using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;
using WebAppERP.Infrastructure;

namespace WebAppERP.Business.Seguranca;

// Regras de negocio da empresa (tbSegEmpresas, uma linha so): nome e logo
// exibidos no menu lateral, no titulo das paginas e no favicon.
// A leitura com cache para o layout fica no EmpresaService.
public class EmpresaBusiness
{
    private readonly AppDbContext _db;
    private readonly ArquivoService _arquivo;

    private const long TamanhoMaxLogo = 5L * 1024 * 1024; // 5 MB
    private const string PastaLogo = "uploads/empresa";

    public EmpresaBusiness(AppDbContext db, ArquivoService arquivo)
    {
        _db = db;
        _arquivo = arquivo;
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
        _arquivo.ValidarImagem(logo, TamanhoMaxLogo, "A logo");

        var empresa = await _db.Empresa.OrderBy(e => e.IdEmpresa).FirstOrDefaultAsync();
        if (empresa == null)
        {
            empresa = new Empresa();
            _db.Empresa.Add(empresa);
        }
        else
        {
        }

        empresa.NmEmpresa = nome;

        // A logo so vai para o disco depois de todas as validacoes.
        var logoAntiga = empresa.DsLogo;
        string? novaLogo = null;
        if (temLogoNova)
        {
            novaLogo = await _arquivo.Gravar(logo!, PastaLogo);
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
            _arquivo.Remover(novaLogo, PastaLogo);
            throw;
        }

        // Remover so apaga logos enviadas (nunca a logo padrao em images/).
        if (logoAntiga != empresa.DsLogo)
            _arquivo.Remover(logoAntiga, PastaLogo);

        return empresa;
    }
}
