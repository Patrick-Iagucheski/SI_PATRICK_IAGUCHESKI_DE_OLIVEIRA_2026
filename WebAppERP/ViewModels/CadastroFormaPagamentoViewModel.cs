namespace WebAppERP.ViewModels;

// Dados do componente de cadastro de forma de pagamento (ViewComponents/CadastroFormaPagamentoViewComponent).
public class CadastroFormaPagamentoViewModel
{
    // Valores do formulario (preenchidos pelo script ao editar).
    public FormaPagamento FormaForm { get; set; } = new();

    // "post": Salvar grava e recarrega a tela de cadastro.
    // "ajax": Salvar grava e dispara o evento 'forma:salva', para telas que
    //         abriram o cadastro pela pesquisa.
    public string Modo { get; set; } = "post";

    // O script do componente entra so na primeira vez que ele aparece na pagina.
    public bool IncluirScript { get; set; }
}
