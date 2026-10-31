using WebAppERP.Models;

namespace WebAppERP.ViewModels;

// Dados do componente de cadastro de condicao de pagamento
// (ViewComponents/CadastroCondicaoPagamentoViewComponent).
public class CadastroCondicaoPagamentoViewModel
{
    public FinCondicaoPagamento CondicaoForm { get; set; } = new();

    // Formas de pagamento ativas, para o select de cada parcela.
    public List<FinFormaPagamento> Formas { get; set; } = [];

    // "post": Salvar faz o submit normal da tela de Condicoes de Pagamento.
    // "ajax": Salvar grava por fetch e dispara o evento 'condicao:salva',
    //         para telas que so precisam da condicao nova (ex.: Fornecedor).
    public string Modo { get; set; } = "post";

    // O script do componente entra so na primeira vez que ele aparece na pagina.
    public bool IncluirScript { get; set; }
}
