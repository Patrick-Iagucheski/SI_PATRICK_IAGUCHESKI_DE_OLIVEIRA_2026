using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebAppERP.Business.Gerais;

namespace WebAppERP.Pages.Gerais;

// Handlers AJAX do componente CampoCidade (ViewComponents/CampoCidadeViewComponent):
// busca da droplist de cidade/estado/pais e cadastro rapido dos tres.
// Todas as telas com campo de cidade (Clientes, Funcionarios, Fornecedores,
// Transportadores, Compra) usam estes mesmos handlers. As regras ficam nas
// classes Business, as mesmas das telas de Cidades, Estados e Paises.
public class LocalidadesModel : PaginaBase
{
    private readonly CidadeBusiness _cidade;
    private readonly EstadoBusiness _estado;
    private readonly PaisBusiness _pais;

    public LocalidadesModel(CidadeBusiness cidade, EstadoBusiness estado, PaisBusiness pais)
    {
        _cidade = cidade;
        _estado = estado;
        _pais = pais;
    }

    public IActionResult OnGet() => RedirectToPage("/Gerais/Cidades");

    // ============================================================
    // Busca da droplist (cidade / estado / pais)
    // ============================================================
    // id: usado ao editar um registro que so tem o idCidade, para exibir o nome.
    public async Task<IActionResult> OnGetBuscarCidades(int? id, string? termo)
    {
        var lista = await _cidade.Buscar(id, termo);
        return new JsonResult(lista.Select(c => new { id = c.IdCidade, nome = c.NmCidade, detalhe = c.Estado?.SgUF }));
    }

    public async Task<IActionResult> OnGetBuscarEstados(string? termo)
    {
        var lista = await _estado.Buscar(termo);
        return new JsonResult(lista.Select(e => new { id = e.IdEstado, nome = e.NmEstado, detalhe = e.SgUF }));
    }

    public async Task<IActionResult> OnGetBuscarPaises(string? termo)
    {
        var lista = await _pais.Buscar(termo);
        return new JsonResult(lista.Select(p => new { id = p.IdPais, nome = p.NmPais, detalhe = p.SgPais }));
    }

    // ============================================================
    // Cadastro rapido de Cidade
    // ============================================================
    public class CidadeRapidoDto
    {
        public string NmCidade { get; set; } = string.Empty;
        public int IdEstado { get; set; }
    }

    public Task<IActionResult> OnPostCidadeRapido([FromBody] CidadeRapidoDto dto) =>
        ExecutarJson(async () =>
        {
            var cidade = await _cidade.Salvar(new Cidade { NmCidade = dto?.NmCidade ?? string.Empty, IdEstado = dto?.IdEstado ?? 0 });
            return new { sucesso = true, id = cidade.IdCidade, nome = cidade.NmCidade, uf = cidade.Estado?.SgUF };
        });

    // ============================================================
    // Cadastro rapido de Estado
    // ============================================================
    public class EstadoRapidoDto
    {
        public string SgUF { get; set; } = string.Empty;
        public string NmEstado { get; set; } = string.Empty;
        public int IdPais { get; set; }
    }

    public Task<IActionResult> OnPostEstadoRapido([FromBody] EstadoRapidoDto dto) =>
        ExecutarJson(async () =>
        {
            var estado = await _estado.Salvar(new Estado
            {
                SgUF = dto?.SgUF ?? string.Empty,
                NmEstado = dto?.NmEstado ?? string.Empty,
                IdPais = dto?.IdPais ?? 0
            });
            return new { sucesso = true, id = estado.IdEstado, nome = estado.NmEstado, uf = estado.SgUF };
        });

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

    public Task<IActionResult> OnPostPaisRapido([FromBody] PaisRapidoDto dto) =>
        ExecutarJson(async () =>
        {
            var pais = await _pais.Salvar(new Pais
            {
                NmPais = dto?.NmPais ?? string.Empty,
                SgPais = dto?.SgPais ?? string.Empty,
                NrDDI = dto?.NrDDI,
                DsMoeda = dto?.DsMoeda
            });
            return new { sucesso = true, id = pais.IdPais, nome = pais.NmPais };
        });
}
