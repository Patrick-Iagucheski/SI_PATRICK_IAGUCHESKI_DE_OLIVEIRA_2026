namespace WebAppERP.ViewModels;

// Dados do componente de cadastro de cliente (ViewComponents/CadastroClienteViewComponent).
public class CadastroClienteViewModel
{
    // Valores do formulario (preenchidos pelo script ao editar).
    public Cliente ClienteForm { get; set; } = new();

    // Condicoes de pagamento ja cadastradas (so ativas): opcoes do campo de condicao.
    public List<CondicaoPagamento> Condicoes { get; set; } = [];

    // "post": Salvar grava e recarrega a tela de cadastro.
    // "ajax": Salvar grava e dispara o evento 'cliente:salvo', para telas que
    //         abriram o cadastro pela pesquisa.
    public string Modo { get; set; } = "post";

    // O script do componente entra so na primeira vez que ele aparece na pagina.
    public bool IncluirScript { get; set; }
}
