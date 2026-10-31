namespace WebAppERP.ViewModels;

// Dados do componente CampoCidade (ViewComponents/CampoCidadeViewComponent).
public class CampoCidadeViewModel
{
    // name do input oculto que vai no POST da tela (ex.: "ClienteForm.IdCidade").
    public string Nome { get; set; } = string.Empty;

    // id do input oculto; o texto exibido usa Id + "Nome" e a lista Id + "NomeLista".
    // Mantem o id que o JS de cada tela ja usava (ex.: "formIdCidade", "fCidade").
    public string Id { get; set; } = string.Empty;

    public int? IdCidade { get; set; }

    // "Cidade / UF" da cidade ja escolhida.
    public string? NomeCidade { get; set; }

    // Os modais de cadastro rapido e o script entram so na primeira vez que o
    // componente aparece na pagina (ids unicos).
    public bool IncluirModais { get; set; }
}
