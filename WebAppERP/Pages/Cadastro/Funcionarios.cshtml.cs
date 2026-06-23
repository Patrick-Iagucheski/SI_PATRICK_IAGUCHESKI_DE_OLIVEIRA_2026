using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;
using WebAppERP.Models;

namespace WebAppERP.Pages.Cadastro;

public class FuncionariosModel : PageModel
{
    private readonly AppDbContext _db;

    public FuncionariosModel(AppDbContext db) => _db = db;

    public List<GerFuncionario> Funcionarios { get; set; } = [];
    public List<GerCidade> Cidades { get; set; } = [];
    public List<GerEstado> Estados { get; set; } = [];
    public List<GerPais> Paises { get; set; } = [];
    public List<GerCargo> Cargos { get; set; } = [];

    [BindProperty]
    public GerFuncionario FuncionarioForm { get; set; } = new();

    public async Task OnGetAsync()
    {
        // Por padrao a listagem mostra apenas ativos (checkbox de inativos desmarcada).
        Funcionarios = await _db.Funcionarios
            .Include(f => f.Cidade)
            .ThenInclude(c => c!.Estado)
            .Include(f => f.Cargo)
            .Where(f => f.FlAtivo)
            .OrderBy(f => f.NmFuncionario)
            .ToListAsync();

        Cidades = await _db.Cidades.Include(c => c.Estado).OrderBy(c => c.NmCidade).ToListAsync();
        Estados = await _db.Estados.OrderBy(e => e.NmEstado).ToListAsync();
        Paises = await _db.Paises.OrderBy(p => p.NmPais).ToListAsync();

        // Apenas cargos ativos no dropdown.
        Cargos = await _db.Cargos.Where(c => c.FlAtivo).OrderBy(c => c.NmCargo).ToListAsync();
    }

    private static string? SomenteNumeros(string? valor) =>
        string.IsNullOrEmpty(valor) ? valor : new string(valor.Where(char.IsDigit).ToArray());

    public async Task<IActionResult> OnPostAsync()
    {
        // Remove a mascara antes de salvar (banco guarda so os numeros)
        FuncionarioForm.NrCpf = SomenteNumeros(FuncionarioForm.NrCpf) ?? string.Empty;
        FuncionarioForm.NrTelefone = SomenteNumeros(FuncionarioForm.NrTelefone);
        FuncionarioForm.NrCEP = SomenteNumeros(FuncionarioForm.NrCEP);

        // O cargo agora vem do dropdown (idCargo). A coluna dsCargo (NOT NULL)
        // e mantida sincronizada com o nome do cargo selecionado.
        string nomeCargo = "";
        if (FuncionarioForm.IdCargo.HasValue)
        {
            var cargoSel = await _db.Cargos.FindAsync(FuncionarioForm.IdCargo.Value);
            nomeCargo = cargoSel?.NmCargo ?? "";
        }

        if (FuncionarioForm.IdFuncionario == 0)
        {
            FuncionarioForm.DsCargo = nomeCargo;
            FuncionarioForm.DtCriacao = DateTime.Now;
            _db.Funcionarios.Add(FuncionarioForm);
        }
        else
        {
            var existing = await _db.Funcionarios.FindAsync(FuncionarioForm.IdFuncionario);
            if (existing != null)
            {
                existing.NmFuncionario = FuncionarioForm.NmFuncionario;
                existing.NmApelido = FuncionarioForm.NmApelido;
                existing.NrCpf = FuncionarioForm.NrCpf;
                existing.NrRG = FuncionarioForm.NrRG;
                existing.DsSexo = FuncionarioForm.DsSexo;
                existing.NrTelefone = FuncionarioForm.NrTelefone;
                existing.DsEmail = FuncionarioForm.DsEmail;
                existing.DsEndereco = FuncionarioForm.DsEndereco;
                existing.DsBairro = FuncionarioForm.DsBairro;
                existing.NrNumero = FuncionarioForm.NrNumero;
                existing.DsComplemento = FuncionarioForm.DsComplemento;
                existing.IdCidade = FuncionarioForm.IdCidade;
                existing.NrCEP = FuncionarioForm.NrCEP;
                existing.DtNascimento = FuncionarioForm.DtNascimento;
                existing.DtAdmissao = FuncionarioForm.DtAdmissao;
                existing.DtDemissao = FuncionarioForm.DtDemissao;
                existing.IdCargo = FuncionarioForm.IdCargo;
                existing.DsCargo = nomeCargo;
                existing.VlSalario = FuncionarioForm.VlSalario;
                existing.VlComissao = FuncionarioForm.VlComissao;
                existing.DsObservacao = FuncionarioForm.DsObservacao;
                existing.FlAtivo = FuncionarioForm.FlAtivo;
                existing.DtEdicao = DateTime.Now;
            }
        }

        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var func = await _db.Funcionarios.FindAsync(id);
        if (func != null)
        {
            _db.Funcionarios.Remove(func);
            await _db.SaveChangesAsync();
        }
        return RedirectToPage();
    }

    // ============================================================
    // Cadastro rapido de Cargo via AJAX
    // ============================================================
    public class CargoRapidoDto
    {
        public string NmCargo { get; set; } = string.Empty;
        public string? DsCargo { get; set; }
        public decimal? VlComissaoPadrao { get; set; }
    }

    public async Task<IActionResult> OnPostCargoRapidoAsync([FromBody] CargoRapidoDto dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.NmCargo))
            return new JsonResult(new { sucesso = false, mensagem = "Informe o nome do cargo." });

        var nome = dto.NmCargo.Trim();

        bool existe = await _db.Cargos.AnyAsync(c => c.NmCargo == nome);
        if (existe)
            return new JsonResult(new { sucesso = false, mensagem = "Ja existe um cargo com esse nome." });

        if (dto.VlComissaoPadrao is < 0 or > 999.99m)
            return new JsonResult(new { sucesso = false, mensagem = "Comissao padrao invalida." });

        var cargo = new GerCargo
        {
            NmCargo = nome,
            DsCargo = string.IsNullOrWhiteSpace(dto.DsCargo) ? null : dto.DsCargo.Trim(),
            VlComissaoPadrao = dto.VlComissaoPadrao,
            FlAtivo = true,
            DtCriacao = DateTime.Now
        };

        _db.Cargos.Add(cargo);
        await _db.SaveChangesAsync();

        return new JsonResult(new { sucesso = true, id = cargo.IdCargo, nome = cargo.NmCargo });
    }

    // ============================================================
    // Cadastro rapido de Cidade via AJAX
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
            return new JsonResult(new { sucesso = false, mensagem = "Estado invalido." });

        var nome = dto.NmCidade.Trim();
        bool existe = await _db.Cidades.AnyAsync(c => c.NmCidade == nome && c.IdEstado == dto.IdEstado);
        if (existe)
            return new JsonResult(new { sucesso = false, mensagem = "Ja existe uma cidade com esse nome neste estado." });

        var cidade = new GerCidade
        {
            NmCidade = nome,
            IdEstado = dto.IdEstado,
            DtCriacao = DateTime.Now
        };

        _db.Cidades.Add(cidade);
        await _db.SaveChangesAsync();

        return new JsonResult(new
        {
            sucesso = true,
            id = cidade.IdCidade,
            nome = cidade.NmCidade,
            uf = estado.SgUF
        });
    }

    // ============================================================
    // Cadastro rapido de Estado via AJAX
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
            return new JsonResult(new { sucesso = false, mensagem = "Selecione o pais." });

        var pais = await _db.Paises.FindAsync(dto.IdPais);
        if (pais is null)
            return new JsonResult(new { sucesso = false, mensagem = "Pais invalido." });

        var uf = dto.SgUF.Trim().ToUpper();
        bool existe = await _db.Estados.AnyAsync(e => e.SgUF == uf && e.IdPais == dto.IdPais);
        if (existe)
            return new JsonResult(new { sucesso = false, mensagem = "Ja existe um estado com essa UF neste pais." });

        var estado = new GerEstado
        {
            SgUF = uf,
            NmEstado = dto.NmEstado.Trim(),
            IdPais = dto.IdPais,
            DtCriacao = DateTime.Now
        };

        _db.Estados.Add(estado);
        await _db.SaveChangesAsync();

        return new JsonResult(new
        {
            sucesso = true,
            id = estado.IdEstado,
            nome = estado.NmEstado,
            uf = estado.SgUF
        });
    }

    // ============================================================
    // Cadastro rapido de Pais via AJAX
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
            return new JsonResult(new { sucesso = false, mensagem = "Informe o nome do pais." });

        if (string.IsNullOrWhiteSpace(dto.SgPais))
            return new JsonResult(new { sucesso = false, mensagem = "Informe a sigla do pais." });

        var nome = dto.NmPais.Trim();
        bool existe = await _db.Paises.AnyAsync(p => p.NmPais == nome);
        if (existe)
            return new JsonResult(new { sucesso = false, mensagem = "Ja existe um pais com esse nome." });

        var pais = new GerPais
        {
            NmPais = nome,
            SgPais = dto.SgPais.Trim().ToUpper(),
            NrDDI = string.IsNullOrWhiteSpace(dto.NrDDI) ? null : SomenteNumeros(dto.NrDDI),
            DsMoeda = string.IsNullOrWhiteSpace(dto.DsMoeda) ? null : dto.DsMoeda.Trim()
        };

        _db.Paises.Add(pais);
        await _db.SaveChangesAsync();

        return new JsonResult(new { sucesso = true, id = pais.IdPais, nome = pais.NmPais });
    }

    // ============================================================
    // Busca de funcionarios via AJAX (modal de pesquisa)
    // Pesquisa por: codigo, nome, apelido ou CPF
    // ============================================================
    public async Task<IActionResult> OnGetBuscarFuncionariosAsync(
        int? id, string? nome, string? apelido, string? cpf, bool incluirInativos = false)
    {
        var query = _db.Funcionarios.Include(f => f.Cargo).AsQueryable();

        // Checkbox desmarcada => apenas ativos. Marcada => apenas inativos.
        if (incluirInativos)
            query = query.Where(f => !f.FlAtivo);
        else
            query = query.Where(f => f.FlAtivo);

        // Todos os filtros sao opcionais e combinaveis.
        if (id.HasValue)
            query = query.Where(f => f.IdFuncionario == id.Value);

        if (!string.IsNullOrWhiteSpace(nome))
        {
            var n = nome.Trim();
            query = query.Where(f => f.NmFuncionario.Contains(n));
        }

        if (!string.IsNullOrWhiteSpace(apelido))
        {
            var a = apelido.Trim();
            query = query.Where(f => f.NmApelido != null && f.NmApelido.Contains(a));
        }

        if (!string.IsNullOrWhiteSpace(cpf))
        {
            var cpfDigitos = new string(cpf.Where(char.IsDigit).ToArray());
            if (cpfDigitos != "")
                query = query.Where(f => f.NrCpf.Contains(cpfDigitos));
        }

        var lista = await query
            .OrderBy(f => f.NmFuncionario)
            .Take(100)
            .Select(f => new
            {
                id = f.IdFuncionario,
                nome = f.NmFuncionario,
                apelido = f.NmApelido,
                cpf = f.NrCpf,
                rg = f.NrRG,
                sexo = f.DsSexo,
                cargoNome = f.Cargo != null ? f.Cargo.NmCargo : f.DsCargo,
                idCargo = f.IdCargo,
                tel = f.NrTelefone,
                email = f.DsEmail,
                end = f.DsEndereco,
                numero = f.NrNumero,
                complemento = f.DsComplemento,
                bairro = f.DsBairro,
                idCidade = f.IdCidade,
                cep = f.NrCEP,
                nasc = f.DtNascimento,
                adm = f.DtAdmissao,
                dem = f.DtDemissao,
                sal = f.VlSalario,
                com = f.VlComissao,
                obs = f.DsObservacao,
                ativo = f.FlAtivo
            })
            .ToListAsync();

        return new JsonResult(lista);
    }
}
