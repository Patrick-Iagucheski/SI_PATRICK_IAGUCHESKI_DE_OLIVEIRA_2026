using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;
using WebAppERP.Models;

namespace WebAppERP.Pages.Cadastro;

// Handlers AJAX do componente CampoCidade (ViewComponents/CampoCidadeViewComponent):
// busca da droplist de cidade/estado/pais e cadastro rapido dos tres.
// Todas as telas com campo de cidade (Clientes, Funcionarios, Fornecedores,
// Transportadores, Compra) usam estes mesmos handlers.
public class LocalidadesModel : PageModel
{
    private readonly AppDbContext _db;

    public LocalidadesModel(AppDbContext db) => _db = db;

    public IActionResult OnGet() => RedirectToPage("/Cadastro/Cidades");

    // Comparacao de nomes nas buscas e na checagem de duplicados: LIKE '%termo%'
    // sem diferenciar maiusculas nem acentos ("sao jose" encontra "São José").
    // O banco usa SQL_Latin1_General_CP1_CI_AS (sensivel a acento), por isso a
    // collation e forcada so na comparacao.
    private const string CollationSemAcento = "SQL_Latin1_General_CP1_CI_AI";

    // Monta o padrao %termo% escapando os curingas digitados pelo usuario.
    private static string LikeContem(string termo) =>
        "%" + termo.Trim().Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]") + "%";

    private static string? SomenteNumeros(string? valor) =>
        string.IsNullOrEmpty(valor) ? valor : new string(valor.Where(char.IsDigit).ToArray());

    // ============================================================
    // Busca da droplist (cidade / estado / pais)
    // ============================================================
    // id: usado ao editar um registro que so tem o idCidade, para exibir o nome.
    public async Task<IActionResult> OnGetBuscarCidadesAsync(int? id, string? termo)
    {
        var query = _db.Cidades.AsQueryable();
        if (id.HasValue)
            query = query.Where(c => c.IdCidade == id.Value);
        else if (!string.IsNullOrWhiteSpace(termo))
        {
            var padrao = LikeContem(termo);
            var t = termo.Trim();
            query = query.Where(c => EF.Functions.Like(EF.Functions.Collate(c.NmCidade, CollationSemAcento), padrao)
                                  || (c.Estado != null && c.Estado.SgUF == t));
        }

        var lista = await query
            .OrderBy(c => c.NmCidade)
            .Take(50)
            .Select(c => new { id = c.IdCidade, nome = c.NmCidade, detalhe = c.Estado != null ? c.Estado.SgUF : null })
            .ToListAsync();
        return new JsonResult(lista);
    }

    public async Task<IActionResult> OnGetBuscarEstadosAsync(string? termo)
    {
        var query = _db.Estados.AsQueryable();
        if (!string.IsNullOrWhiteSpace(termo))
        {
            var padrao = LikeContem(termo);
            var t = termo.Trim();
            query = query.Where(e => EF.Functions.Like(EF.Functions.Collate(e.NmEstado, CollationSemAcento), padrao)
                                  || e.SgUF == t);
        }

        var lista = await query
            .OrderBy(e => e.NmEstado)
            .Take(50)
            .Select(e => new { id = e.IdEstado, nome = e.NmEstado, detalhe = e.SgUF })
            .ToListAsync();
        return new JsonResult(lista);
    }

    public async Task<IActionResult> OnGetBuscarPaisesAsync(string? termo)
    {
        var query = _db.Paises.AsQueryable();
        if (!string.IsNullOrWhiteSpace(termo))
        {
            var padrao = LikeContem(termo);
            var t = termo.Trim();
            query = query.Where(p => EF.Functions.Like(EF.Functions.Collate(p.NmPais, CollationSemAcento), padrao)
                                  || p.SgPais == t);
        }

        var lista = await query
            .OrderBy(p => p.NmPais)
            .Take(50)
            .Select(p => new { id = p.IdPais, nome = p.NmPais, detalhe = p.SgPais })
            .ToListAsync();
        return new JsonResult(lista);
    }

    // ============================================================
    // Cadastro rapido de Cidade
    // ============================================================
    public class CidadeRapidoDto
    {
        public string NmCidade { get; set; } = string.Empty;
        public int IdEstado { get; set; }
    }

    public async Task<IActionResult> OnPostCidadeRapidoAsync([FromBody] CidadeRapidoDto dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.NmCidade))
            return new JsonResult(new { sucesso = false, mensagem = "Informe o nome da cidade." });
        if (dto.IdEstado <= 0)
            return new JsonResult(new { sucesso = false, mensagem = "Selecione o estado." });

        var estado = await _db.Estados.FindAsync(dto.IdEstado);
        if (estado is null)
            return new JsonResult(new { sucesso = false, mensagem = "Estado inválido." });

        var nome = dto.NmCidade.Trim();
        var padrao = LikeContem(nome);
        var parecida = await _db.Cidades
            .Where(c => c.IdEstado == dto.IdEstado
                     && EF.Functions.Like(EF.Functions.Collate(c.NmCidade, CollationSemAcento), padrao))
            .Select(c => c.NmCidade)
            .FirstOrDefaultAsync();
        if (parecida != null)
            return new JsonResult(new { sucesso = false, mensagem = $"Já existe a cidade \"{parecida}\" neste estado. Utilize a pesquisa para selecioná-la." });

        var cidade = new GerCidade { NmCidade = nome, IdEstado = dto.IdEstado, DtCriacao = DateTime.Now };
        _db.Cidades.Add(cidade);
        await _db.SaveChangesAsync();

        return new JsonResult(new { sucesso = true, id = cidade.IdCidade, nome = cidade.NmCidade, uf = estado.SgUF });
    }

    // ============================================================
    // Cadastro rapido de Estado
    // ============================================================
    public class EstadoRapidoDto
    {
        public string SgUF { get; set; } = string.Empty;
        public string NmEstado { get; set; } = string.Empty;
        public int IdPais { get; set; }
    }

    public async Task<IActionResult> OnPostEstadoRapidoAsync([FromBody] EstadoRapidoDto dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.NmEstado))
            return new JsonResult(new { sucesso = false, mensagem = "Informe o nome do estado." });
        if (string.IsNullOrWhiteSpace(dto.SgUF) || dto.SgUF.Trim().Length != 2)
            return new JsonResult(new { sucesso = false, mensagem = "Informe a UF (2 letras)." });
        if (dto.IdPais <= 0)
            return new JsonResult(new { sucesso = false, mensagem = "Selecione o país." });

        var pais = await _db.Paises.FindAsync(dto.IdPais);
        if (pais is null)
            return new JsonResult(new { sucesso = false, mensagem = "País inválido." });

        var uf = dto.SgUF.Trim().ToUpper();
        var padrao = LikeContem(dto.NmEstado);
        var parecido = await _db.Estados
            .Where(e => e.IdPais == dto.IdPais
                     && (e.SgUF == uf
                         || EF.Functions.Like(EF.Functions.Collate(e.NmEstado, CollationSemAcento), padrao)))
            .Select(e => e.NmEstado + " / " + e.SgUF)
            .FirstOrDefaultAsync();
        if (parecido != null)
            return new JsonResult(new { sucesso = false, mensagem = $"Já existe o estado \"{parecido}\" neste país. Utilize a pesquisa para selecioná-lo." });

        var estado = new GerEstado { SgUF = uf, NmEstado = dto.NmEstado.Trim(), IdPais = dto.IdPais, DtCriacao = DateTime.Now };
        _db.Estados.Add(estado);
        await _db.SaveChangesAsync();

        return new JsonResult(new { sucesso = true, id = estado.IdEstado, nome = estado.NmEstado, uf = estado.SgUF });
    }

    // ============================================================
    // Cadastro rapido de Pais
    // ============================================================
    public class PaisRapidoDto
    {
        public string NmPais { get; set; } = string.Empty;
        public string SgPais { get; set; } = string.Empty;
        public string? NrDDI { get; set; }
        public string? DsMoeda { get; set; }
    }

    public async Task<IActionResult> OnPostPaisRapidoAsync([FromBody] PaisRapidoDto dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.NmPais))
            return new JsonResult(new { sucesso = false, mensagem = "Informe o nome do país." });
        if (string.IsNullOrWhiteSpace(dto.SgPais))
            return new JsonResult(new { sucesso = false, mensagem = "Informe a sigla do país." });

        var nome = dto.NmPais.Trim();
        var sigla = dto.SgPais.Trim().ToUpper();
        var padrao = LikeContem(nome);
        var parecido = await _db.Paises
            .Where(p => p.SgPais == sigla
                     || EF.Functions.Like(EF.Functions.Collate(p.NmPais, CollationSemAcento), padrao))
            .Select(p => p.NmPais)
            .FirstOrDefaultAsync();
        if (parecido != null)
            return new JsonResult(new { sucesso = false, mensagem = $"Já existe o país \"{parecido}\". Utilize a pesquisa para selecioná-lo." });

        var pais = new GerPais
        {
            NmPais = nome,
            SgPais = sigla,
            NrDDI = string.IsNullOrWhiteSpace(dto.NrDDI) ? null : SomenteNumeros(dto.NrDDI),
            DsMoeda = string.IsNullOrWhiteSpace(dto.DsMoeda) ? null : dto.DsMoeda.Trim()
        };
        _db.Paises.Add(pais);
        await _db.SaveChangesAsync();

        return new JsonResult(new { sucesso = true, id = pais.IdPais, nome = pais.NmPais });
    }
}
