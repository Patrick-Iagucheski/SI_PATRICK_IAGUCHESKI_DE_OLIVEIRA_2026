using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebAppERP.Business.Gerais;

namespace WebAppERP.Pages.Gerais;

// Tela de Fornecedores: so conversa com a tela. As regras ficam em FornecedorBusiness.
public class FornecedoresModel : PaginaBase
{
    private readonly FornecedorBusiness _fornecedor;

    public FornecedoresModel(FornecedorBusiness fornecedor) => _fornecedor = fornecedor;

    public List<Fornecedor> Fornecedores { get; set; } = [];

    // Cidades, estados e paises do modal sao carregados pelo proprio
    // componente (ViewComponents/CadastroFornecedorViewComponent).
    [BindProperty]
    public Fornecedor FornecedorForm { get; set; } = new();

    public async Task OnGet()
    {
        // Por padrao a listagem mostra apenas ativos (checkbox de inativos desmarcada).
        Fornecedores = await _fornecedor.Listar();
    }

    public Task<IActionResult> OnPost() =>
        Executar(() => _fornecedor.Salvar(FornecedorForm),
            FornecedorForm.IdFornecedor == 0 ? "Fornecedor cadastrado com sucesso." : "Fornecedor atualizado com sucesso.");

    // Cadastro pelo componente em modo AJAX (ex.: Novo/Editar da pesquisa de
    // fornecedor na Compra). Mesmas regras do OnPost, mas responde JSON
    // para a tela de origem nao ser recarregada.
    public Task<IActionResult> OnPostSalvarAjax() =>
        ExecutarJson(async () =>
        {
            var fornecedor = await _fornecedor.Salvar(FornecedorForm);
            return new
            {
                sucesso = true,
                id = fornecedor.IdFornecedor,
                razaoSocial = fornecedor.DsRazaoSocial,
                nomeFantasia = fornecedor.DsNomeFantasia
            };
        });

    public Task<IActionResult> OnPostExcluir(int id) =>
        Executar(() => _fornecedor.Excluir(id), "Fornecedor excluído com sucesso.");

    // ============================================================
    // Busca de fornecedores via AJAX (filtros da pagina)
    // Pesquisa por: codigo, razao social, nome fantasia ou CPF/CNPJ
    // ============================================================
    public async Task<IActionResult> OnGetBuscarFornecedores(
        int? id, string? razao, string? fantasia, string? documento, bool incluirInativos = false)
    {
        var lista = await _fornecedor.Buscar(id, razao, fantasia, documento, incluirInativos);
        return new JsonResult(lista.Select(f => new
        {
            id = f.IdFornecedor,
            tipoPessoa = f.TpPessoa,
            razaoSocial = f.DsRazaoSocial,
            nomeFantasia = f.DsNomeFantasia,
            cpf = f.NrCpf,
            cnpj = f.NrCNPJ,
            documento = f.NrDocumento,
            inscEstadual = f.NrInscEstadual,
            inscMunicipal = f.NrInscMunicipal,
            rg = f.NrRG,
            dataNascimento = f.DtNascimento,
            email = f.DsEmail,
            observacao = f.DsObservacao,
            cep = f.NrCEP,
            endereco = f.DsEndereco,
            numero = f.NrNumero,
            complemento = f.DsComplemento,
            bairro = f.DsBairro,
            idCidade = f.IdCidade,
            idCondicaoPagamento = f.IdCondicaoPagamento,
            cidadeNome = Module.CidadeUf(f.Cidade),
            telefoneFixo = f.NrTelefoneFixo,
            celular = f.NrCelular,
            site = f.DsSite,
            banco = f.DsBanco,
            agencia = f.NrAgencia,
            tipoConta = f.TpConta,
            conta = f.NrConta,
            digitoConta = f.NrDigitoConta,
            tipoChavePix = f.TpChavePix,
            chavePix = f.DsChavePix,
            ativo = f.FlAtivo
        }));
    }
}
