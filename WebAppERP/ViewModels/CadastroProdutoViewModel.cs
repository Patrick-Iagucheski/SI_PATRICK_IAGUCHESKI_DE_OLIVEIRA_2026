
namespace WebAppERP.ViewModels;

// Dados do componente de cadastro de produto (ViewComponents/CadastroProdutoViewComponent).
public class CadastroProdutoViewModel
{
    public Produto ProdutoForm { get; set; } = new();

    // Apenas marcas e categorias ativas (opcoes dos campos de marca e categoria).
    public List<Marca> Marcas { get; set; } = [];
    public List<Categoria> Categorias { get; set; } = [];

    // "post": Salvar faz o submit normal da tela de Produtos.
    // "ajax": Salvar grava por fetch e dispara o evento 'produto:salvo',
    //         para telas que so precisam do produto novo (ex.: Compra).
    public string Modo { get; set; } = "post";

    // O script do componente entra so na primeira vez que ele aparece na pagina.
    public bool IncluirScript { get; set; }
}
