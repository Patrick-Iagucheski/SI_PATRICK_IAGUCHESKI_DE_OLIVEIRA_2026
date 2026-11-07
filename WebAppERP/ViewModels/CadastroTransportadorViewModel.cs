namespace WebAppERP.ViewModels;

// Dados do componente de cadastro de transportadora (ViewComponents/CadastroTransportadorViewComponent).
public class CadastroTransportadorViewModel
{
    // Valores do formulario (preenchidos pelo script ao editar).
    public Transportador TransportadorForm { get; set; } = new();

    // "post": Salvar grava e recarrega a tela de Transportadores.
    // "ajax": Salvar grava e dispara o evento 'transportador:salvo', para
    //         telas que abriram o cadastro pela pesquisa (ex.: Compra).
    public string Modo { get; set; } = "post";

    // O script do componente entra so na primeira vez que ele aparece na pagina.
    public bool IncluirScript { get; set; }
}
