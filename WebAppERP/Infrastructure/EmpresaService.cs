using Microsoft.Extensions.Caching.Memory;
using WebAppERP.Business.Seguranca;

namespace WebAppERP.Infrastructure;

// Nome e logo da empresa exibidos no layout (menu lateral e titulo das paginas).
// O layout roda em toda pagina, entao o valor fica em cache ate a tela de
// Configuracoes salvar (Limpar). A leitura em si fica no EmpresaBusiness.
public class EmpresaService
{
    public const string NomePadrao = "R10 Barber Shop";
    public const string LogoPadrao = "/images/logo.png";
    private const string ChaveCache = "empresa";

    private readonly EmpresaBusiness _empresa;
    private readonly IMemoryCache _cache;

    public EmpresaService(EmpresaBusiness empresa, IMemoryCache cache)
    {
        _empresa = empresa;
        _cache = cache;
    }

    public record DadosEmpresa(string Nome, string Logo);

    public async Task<DadosEmpresa> GetDadosEmpresa()
    {
        if (_cache.TryGetValue(ChaveCache, out DadosEmpresa? dados) && dados != null)
            return dados;

        try
        {
            var empresa = await _empresa.GetEmpresa();
            dados = new DadosEmpresa(
                string.IsNullOrWhiteSpace(empresa?.NmEmpresa) ? NomePadrao : empresa.NmEmpresa,
                string.IsNullOrWhiteSpace(empresa?.DsLogo) ? LogoPadrao : empresa.DsLogo);
        }
        catch (Exception)
        {
            // Banco sem a tabela (Database/CriarEmpresa.sql nao rodado): o
            // sistema continua abrindo com o nome e a logo padrao, sem cache.
            return new DadosEmpresa(NomePadrao, LogoPadrao);
        }

        _cache.Set(ChaveCache, dados);
        return dados;
    }

    public void Limpar() => _cache.Remove(ChaveCache);
}
