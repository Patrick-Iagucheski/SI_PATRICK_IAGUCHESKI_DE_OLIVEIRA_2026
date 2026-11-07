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

    [BindProperty]
    public Transportador TransportadorForm { get; set; } = new();

    public async Task OnGet()
    {
        Transportadores = await _transportador.Listar();
    }

    public Task<IActionResult> OnPost() =>
        Executar(() => _transportador.Salvar(TransportadorForm), TransportadorForm.IdTransportador == 0 ? "Transportador cadastrado com sucesso." : "Transportador atualizado com sucesso.");

    public Task<IActionResult> OnPostExcluir(int id) =>
        Executar(() => _transportador.Excluir(id), "Transportador excluído com sucesso.");

    // Cadastro pelo componente em modo AJAX (ex.: pesquisa de transportadora da
    // Compra). Mesmas regras do OnPost, mas responde JSON para a tela de origem.
    public Task<IActionResult> OnPostSalvarAjax() =>
        ExecutarJson(async () =>
        {
            var t = await _transportador.Salvar(TransportadorForm);
            return new { sucesso = true, id = t.IdTransportador, nome = t.DsRazaoSocial };
        });

    // Pesquisa (componente): codigo ou razao social / CPF / CNPJ, com os dados
    // que a grade e o Editar usam.
    public async Task<IActionResult> OnGetBuscarTransportadores(int? id, string? termo)
    {
        var lista = await _transportador.Pesquisar(id, termo);
        return new JsonResult(lista.Select(t => new
        {
            id = t.IdTransportador, registro = Module.Registro(t),
            nome = t.DsRazaoSocial,
            documento = t.NrCpfCnpj,
            endereco = t.DsEndereco,
            idCidade = t.IdCidade,
            cidadeNome = Module.CidadeUf(t.Cidade),
            inscEstadual = t.NrInscEstadual
        }));
    }
}
