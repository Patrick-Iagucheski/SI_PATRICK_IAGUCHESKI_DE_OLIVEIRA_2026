using WebAppERP.Models;

namespace WebAppERP.ViewModels;

// Dados do componente de cadastro de fornecedor (ViewComponents/CadastroFornecedorViewComponent).
public class CadastroFornecedorViewModel
{
    // Valores do formulario. Na tela de Fornecedores volta preenchido quando
    // a gravacao falha, para o usuario nao perder o que digitou.
    public GerFornecedor FornecedorForm { get; set; } = new();

    // Condicoes de pagamento ja cadastradas (so ativas) para o dropdown.
    public List<FinCondicaoPagamento> Condicoes { get; set; } = [];

    // "post": Salvar faz o submit normal da tela de Fornecedores.
    // "ajax": Salvar grava por fetch e dispara o evento 'fornecedor:salvo',
    //         para telas que so precisam do fornecedor novo (ex.: Compra).
    public string Modo { get; set; } = "post";
}
