using WebAppERP.Models;

namespace WebAppERP.ViewModels;

// Dados do componente de cadastro de produto (ViewComponents/CadastroProdutoViewComponent).
public class CadastroProdutoViewModel
{
    public EstProduto ProdutoForm { get; set; } = new();

    // Apenas marcas e categorias ativas nos dropdowns.
    public List<EstMarca> Marcas { get; set; } = [];
    public List<EstCategoria> Categorias { get; set; } = [];

    // "post": Salvar faz o submit normal da tela de Produtos.
    // "ajax": Salvar grava por fetch e dispara o evento 'produto:salvo',
    //         para telas que so precisam do produto novo (ex.: Compra).
    public string Modo { get; set; } = "post";
}
