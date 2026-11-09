using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebAppERP.Business.Gerais;

namespace WebAppERP.Pages.Gerais;

// Tela de Transportadores: so conversa com a tela. As regras ficam em TransportadorBusiness.
public class TransportadoresModel : PaginaBase
{
    private readonly TransportadorBusiness _transportador;

    public TransportadoresModel(TransportadorBusiness transportador) => _transportador = transportador;

    public List<Transportador> Transportadores { get; set; } = [];

    // Cidades, estados e paises do modal sao carregados pelo proprio
    // componente (ViewComponents/CadastroTransportadorViewComponent).
    [BindProperty]
    public Transportador TransportadorForm { get; set; } = new();

    public async Task OnGet()
    {
        // Por padrao a listagem mostra apenas as ativas (checkbox de inativas desmarcada).
        Transportadores = await _transportador.Listar();
    }

    public Task<IActionResult> OnPost() =>
        Executar(() => _transportador.Salvar(TransportadorForm),
            TransportadorForm.IdTransportador == 0 ? "Transportadora cadastrada com sucesso." : "Transportadora atualizada com sucesso.");

    // Cadastro pelo componente em modo AJAX (ex.: Novo/Editar da pesquisa de
    // transportadora na Compra). Mesmas regras do OnPost, mas responde JSON
    // para a tela de origem nao ser recarregada.
    public Task<IActionResult> OnPostSalvarAjax() =>
        ExecutarJson(async () =>
        {
            var transportador = await _transportador.Salvar(TransportadorForm);
            return new
            {
                sucesso = true,
                id = transportador.IdTransportador,
                razaoSocial = transportador.DsRazaoSocial,
                nomeFantasia = transportador.DsNomeFantasia
            };
        });

    public Task<IActionResult> OnPostExcluir(int id) =>
        Executar(() => _transportador.Excluir(id), "Transportadora excluída com sucesso.");

    // ============================================================
    // Busca de transportadoras via AJAX (filtros da pagina e pesquisa do componente)
    // Pesquisa por: codigo, razao social, nome fantasia ou CPF/CNPJ
    // ============================================================
    public async Task<IActionResult> OnGetBuscarTransportadores(
        int? id, string? razao, string? fantasia, string? documento, bool incluirInativos = false)
    {
        var lista = await _transportador.Buscar(id, razao, fantasia, documento, incluirInativos);
        return new JsonResult(lista.Select(Registro));
    }

    // Transportadora no formato que o componente usa para editar
    // (editarTransportador no Default.cshtml.js): o mesmo para a busca e para as
    // linhas iniciais da grade.
    public static object Registro(Transportador t) => new
    {
        id = t.IdTransportador,
        registro = Module.Registro(t),
        tipoPessoa = t.TpPessoa,
        razaoSocial = t.DsRazaoSocial,
        nomeFantasia = t.DsNomeFantasia,
        cpf = t.NrCpf,
        cnpj = t.NrCNPJ,
        documento = t.NrDocumento,
        inscEstadual = t.NrInscEstadual,
        inscMunicipal = t.NrInscMunicipal,
        rg = t.NrRG,
        dataNascimento = t.DtNascimento?.ToString("yyyy-MM-dd"),
        email = t.DsEmail,
        observacao = t.DsObservacao,
        cep = t.NrCEP,
        endereco = t.DsEndereco,
        numero = t.NrNumero,
        complemento = t.DsComplemento,
        bairro = t.DsBairro,
        idCidade = t.IdCidade,
        cidadeNome = Module.CidadeUf(t.Cidade),
        telefoneFixo = t.NrTelefoneFixo,
        celular = t.NrCelular,
        site = t.DsSite,
        ativo = t.FlAtivo
    };
}
