using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebAppERP.Business.Gerais;

namespace WebAppERP.Pages.Gerais;

// Tela de Clientes: so conversa com a tela. As regras ficam em ClienteBusiness.
public class ClientesModel : PageModel
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

    public async Task<IActionResult> OnPost()
    {
        try
        {
            await _cliente.Salvar(ClienteForm);
            return RedirectToPage();
        }
        catch (ValidacaoException ex)
        {
            // Recarrega a pagina mantendo a mensagem de erro amigavel.
            TempData["ErroCliente"] = ex.MensagemCompleta;
            await OnGet();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostExcluir(int id)
    {
        try
        {
            await _cliente.Excluir(id);
            TempData["Sucesso"] = "Cliente excluído com sucesso.";
        }
        catch (ValidacaoException ex)
        {
            TempData["Erro"] = ex.MensagemCompleta;
        }
        return RedirectToPage();
    }

    // Busca de clientes via AJAX (filtros da pagina)
    // Pesquisa por: codigo, nome, documento (CPF/CNPJ/outro) ou e-mail
    public async Task<IActionResult> OnGetBuscarClientes(
        int? id, string? nome, string? documento, string? email, bool incluirInativos = false)
    {
        var lista = await _cliente.Buscar(id, nome, documento, email, incluirInativos);
        return new JsonResult(lista.Select(c => new
        {
            id = c.IdCliente,
            nome = c.NmCliente,
            tipoPessoa = c.TpPessoa,
            cpf = c.NrCpf,
            cnpj = c.NrCnpj,
            documento = c.NrDocumento,
            rg = c.NrRG,
            tel = c.NrTelefone,
            email = c.DsEmail,
            end = c.DsEndereco,
            numero = c.NrNumero,
            complemento = c.DsComplemento,
            bairro = c.DsBairro,
            idCidade = c.IdCidade,
            cidadeNome = c.Cidade != null ? c.Cidade.NmCidade + "/" + (c.Cidade.Estado != null ? c.Cidade.Estado.SgUF : "") : null,
            cep = c.NrCEP,
            nasc = c.DtNascimento,
            sexo = c.DsSexo,
            obs = c.DsObservacao,
            ativo = c.FlAtivo
        }));
    }
}
