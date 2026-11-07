namespace WebAppERP.ViewModels;

// Dados do componente de cadastro de funcionario (ViewComponents/CadastroFuncionarioViewComponent).
public class CadastroFuncionarioViewModel
{
    // Valores do formulario (preenchidos pelo script ao editar).
    public Funcionario FuncionarioForm { get; set; } = new();

    // Cargos ativos do campo de cargo (a lupa abre a pesquisa de cargo).
    public List<Cargo> Cargos { get; set; } = [];

    // "post": Salvar grava e recarrega a tela de cadastro.
    // "ajax": Salvar grava e dispara o evento 'funcionario:salvo', para telas que
    //         abriram o cadastro pela pesquisa.
    public string Modo { get; set; } = "post";

    // O script do componente entra so na primeira vez que ele aparece na pagina.
    public bool IncluirScript { get; set; }
}
