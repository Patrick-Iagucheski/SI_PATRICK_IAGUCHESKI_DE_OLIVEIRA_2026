using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebAppERP.Business.Gerais;

namespace WebAppERP.Pages.Gerais;

// Tela de Funcionarios: so conversa com a tela. As regras ficam em FuncionarioBusiness.
public class FuncionariosModel : PaginaBase
{
    private readonly FuncionarioBusiness _funcionario;
    private readonly CargoBusiness _cargo;

    public FuncionariosModel(FuncionarioBusiness funcionario, CargoBusiness cargo)
    {
        _funcionario = funcionario;
        _cargo = cargo;
    }

    public List<Funcionario> Funcionarios { get; set; } = [];
    public List<Cargo> Cargos { get; set; } = [];

    [BindProperty]
    public Funcionario FuncionarioForm { get; set; } = new();

    public async Task OnGet()
    {
        // Por padrao a listagem mostra apenas ativos (checkbox de inativos desmarcada).
        Funcionarios = await _funcionario.Listar();

        // Apenas cargos ativos no dropdown.
        Cargos = await _cargo.Listar();
    }

    public Task<IActionResult> OnPost() =>
        Executar(() => _funcionario.Salvar(FuncionarioForm), FuncionarioForm.IdFuncionario == 0 ? "Funcionário cadastrado com sucesso." : "Funcionário atualizado com sucesso.");

    public Task<IActionResult> OnPostExcluir(int id) =>
        Executar(() => _funcionario.Excluir(id), "Funcionário excluído com sucesso.");

    // ============================================================
    // Cadastro rapido de Cargo via AJAX (mesmas regras da tela de Cargos)
    // ============================================================
    public class CargoRapidoDto
    {
        public string NmCargo { get; set; } = string.Empty;
        public string? DsCargo { get; set; }
        public decimal? VlComissaoPadrao { get; set; }
    }

    public Task<IActionResult> OnPostCargoRapido([FromBody] CargoRapidoDto dto) =>
        ExecutarJson(async () =>
        {
            var cargo = await _cargo.Salvar(new Cargo
            {
                NmCargo = dto?.NmCargo ?? string.Empty,
                DsCargo = dto?.DsCargo,
                VlComissaoPadrao = dto?.VlComissaoPadrao,
                FlAtivo = true
            });
            return new { sucesso = true, id = cargo.IdCargo, nome = cargo.NmCargo };
        });

    // ============================================================
    // Busca de funcionarios via AJAX (modal de pesquisa)
    // Pesquisa por: codigo, nome, apelido ou CPF
    // ============================================================
    public async Task<IActionResult> OnGetBuscarFuncionarios(
        int? id, string? nome, string? apelido, string? cpf, bool incluirInativos = false)
    {
        var lista = await _funcionario.Buscar(id, nome, apelido, cpf, incluirInativos);
        return new JsonResult(lista.Select(f => new
        {
            id = f.IdFuncionario,
            nome = f.NmFuncionario,
            apelido = f.NmApelido,
            cpf = f.NrCpf,
            rg = f.NrRG,
            sexo = f.DsSexo,
            cargoNome = f.Cargo?.NmCargo,
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
        }));
    }
}
