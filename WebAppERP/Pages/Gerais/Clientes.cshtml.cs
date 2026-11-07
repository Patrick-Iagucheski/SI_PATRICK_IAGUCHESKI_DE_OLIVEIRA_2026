using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebAppERP.Business.Gerais;

namespace WebAppERP.Pages.Gerais;

// Tela de Clientes: so conversa com a tela. As regras ficam em ClienteBusiness.
public class ClientesModel : PaginaBase
{
    private readonly ClienteBusiness _cliente;

    public ClientesModel(ClienteBusiness cliente) => _cliente = cliente;

    public List<Cliente> Clientes { get; set; } = [];

    [BindProperty]
    public Cliente ClienteForm { get; set; } = new();

    public async Task OnGet()
    {
        // Por padrao a listagem mostra apenas ativos (checkbox de inativos desmarcada).
        Clientes = await _cliente.Listar();
    }

    public Task<IActionResult> OnPost() =>
        Executar(() => _cliente.Salvar(ClienteForm),
            ClienteForm.IdCliente == 0 ? "Cliente cadastrado com sucesso." : "Cliente atualizado com sucesso.");

    // Cadastro pelo componente em modo AJAX (pesquisas de cliente de outras
    // telas). Mesmas regras do OnPost, mas responde JSON para a tela de origem.
    public Task<IActionResult> OnPostSalvarAjax() =>
        ExecutarJson(async () =>
        {
            var c = await _cliente.Salvar(ClienteForm);
            return new { sucesso = true, id = c.IdCliente, nome = c.NmCliente };
        });

    public Task<IActionResult> OnPostExcluir(int id) =>
        Executar(() => _cliente.Excluir(id), "Cliente excluído com sucesso.");

    // Busca de clientes via AJAX (filtros da pagina)
    // Pesquisa por: codigo, nome, documento (CPF/CNPJ/outro) ou e-mail
    public async Task<IActionResult> OnGetBuscarClientes(
        int? id, string? nome, string? documento, string? email, bool incluirInativos = false)
    {
        var lista = await _cliente.Buscar(id, nome, documento, email, incluirInativos);
        return new JsonResult(lista.Select(Registro));
    }

    // Cliente no formato que o componente usa para editar (editarCliente no
    // Default.cshtml.js): o mesmo para a busca e para as linhas iniciais da grade.
    public static object Registro(Cliente c) => new
    {
        id = c.IdCliente,
        nome = c.NmCliente,
        tipoPessoa = c.TpPessoa,
        cpf = c.NrCpf,
        cnpj = c.NrCnpj,
        documento = c.NrDocumento,
        rg = c.NrRG,
        nomeFantasia = c.DsNomeFantasia,
        inscEstadual = c.NrInscEstadual,
        inscMunicipal = c.NrInscMunicipal,
        telefoneFixo = c.NrTelefoneFixo,
        celular = c.NrCelular,
        site = c.DsSite,
        email = c.DsEmail,
        end = c.DsEndereco,
        numero = c.NrNumero,
        complemento = c.DsComplemento,
        bairro = c.DsBairro,
        idCidade = c.IdCidade,
        cidadeNome = Module.CidadeUf(c.Cidade),
        cep = c.NrCEP,
        nasc = c.DtNascimento,
        sexo = c.DsSexo,
        idCondicaoPagamento = c.IdCondicaoPagamento,
        condicaoNome = c.CondicaoPagamento?.DsCondicao,
        obs = c.DsObservacao,
        ativo = c.FlAtivo
    };
}
