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
        FuncionarioForm.NrRG = SomenteNumeros(FuncionarioForm.NrRG);

        // O cargo vem do dropdown (idCargo) e e a unica fonte da verdade:
        // o nome sai da navegacao Cargo.NmCargo na hora de exibir.

        if (FuncionarioForm.IdFuncionario == 0)
        {
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
        if (func == null)
            return RedirectToPage();

        // Filhas: tbOpeAgendamentos.idFuncionario e tbOpeVendas.idFuncionario.
        if (await _db.Agendamentos.AnyAsync(a => a.IdFuncionario == id))
        {
            TempData["Erro"] = "Não é possível excluir: existe agendamento para este funcionário. Inative-o.";
            return RedirectToPage();
        }

        if (await _db.Vendas.AnyAsync(v => v.IdFuncionario == id))
        {
            TempData["Erro"] = "Não é possível excluir: existe venda registrada por este funcionário. Inative-o.";
            return RedirectToPage();
        }

        try
        {
            _db.Funcionarios.Remove(func);
            await _db.SaveChangesAsync();
            TempData["Sucesso"] = "Funcionário excluído com sucesso.";
        }
        catch (DbUpdateException)
        {
            TempData["Erro"] = "Não é possível excluir: existem registros vinculados a este funcionário. Inative-o.";
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
            return new JsonResult(new { sucesso = false, mensagem = "Já existe um cargo com esse nome." });

        if (dto.VlComissaoPadrao is < 0 or > 999.99m)
            return new JsonResult(new { sucesso = false, mensagem = "Comissão padrão inválida." });

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
                cargoNome = f.Cargo != null ? f.Cargo.NmCargo : null,
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
