namespace WebAppERP.ViewModels;

// Dados do componente de cadastro de agendamento (ViewComponents/CadastroAgendamentoViewComponent).
public class CadastroAgendamentoViewModel
{
    // Valores do formulario (preenchidos pelo script ao editar).
    public Agendamento AgendamentoForm { get; set; } = new();

    // Opcoes dos campos (clientes e profissionais ativos, servicos ativos).
    public List<Cliente> Clientes { get; set; } = [];
    public List<Funcionario> Funcionarios { get; set; } = [];
    public List<Servico> Servicos { get; set; } = [];

    // Data que o agendamento novo ja traz (yyyy-MM-dd): o dia da agenda na tela
    // de Agendamentos; hoje nas outras telas.
    public string DataPadrao { get; set; } = "";

    // "post": Salvar grava e recarrega a tela de Agendamentos.
    // "ajax": Salvar grava e dispara o evento 'agendamento:salvo', para telas
    //         que abriram o cadastro pela pesquisa (ex.: Venda).
    public string Modo { get; set; } = "post";

    // O script do componente entra so na primeira vez que ele aparece na pagina.
    public bool IncluirScript { get; set; }
}
