namespace WebAppERP.ViewModels;

// Dados do componente CampoCidade (ViewComponents/CampoCidadeViewComponent).
public class CampoCidadeViewModel
{
    // name do campo que vai no POST da tela (ex.: "ClienteForm.IdCidade").
    public string Nome { get; set; } = string.Empty;

    // id do <select> (o texto exibido pelo module.js usa Id + "Nome").
    // Mantem o id que o JS de cada tela ja usava (ex.: "formIdCidade", "fCidade").
    public string Id { get; set; } = string.Empty;

    public int? IdCidade { get; set; }

    // "Cidade / UF" da cidade ja escolhida.
    public string? NomeCidade { get; set; }

    // Os modais de cadastro rapido e o script entram so na primeira vez que o
    // componente aparece na pagina (ids unicos).
    public bool IncluirModais { get; set; }

    // Sem o campo: so os cadastros e as pesquisas de cidade/estado/pais, para
    // telas que tem campo de estado ou pais (Cidades, Estados, Veiculos).
    public bool SomenteCadastros { get; set; }
}
