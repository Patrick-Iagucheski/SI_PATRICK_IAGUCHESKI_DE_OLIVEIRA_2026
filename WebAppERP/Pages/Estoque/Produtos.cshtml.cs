using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebAppERP.Business.Estoque;

namespace WebAppERP.Pages.Estoque;

// Tela de Produtos: so conversa com a tela. As regras (e a imagem) ficam em ProdutoBusiness.
public class ProdutosModel : PaginaBase
{
    private readonly ProdutoBusiness _produto;
    private readonly MarcaBusiness _marca;
    private readonly CategoriaBusiness _categoria;

    public ProdutosModel(ProdutoBusiness produto, MarcaBusiness marca, CategoriaBusiness categoria)
    {
        _produto = produto;
        _marca = marca;
        _categoria = categoria;
    }

    public List<Produto> Produtos { get; set; } = [];

    // Marcas e categorias do modal sao carregadas pelo proprio componente
    // (ViewComponents/CadastroProdutoViewComponent).
    [BindProperty]
    public Produto ProdutoForm { get; set; } = new();

    // Arquivo de imagem enviado pelo formulario (opcional)
    [BindProperty]
    public IFormFile? ImagemUpload { get; set; }

    public async Task OnGet()
    {
        // Por padrao a listagem mostra apenas ativos (checkbox de inativos desmarcada).
        Produtos = await _produto.Listar();
    }

    public Task<IActionResult> OnPost() =>
        Executar(() => _produto.Salvar(ProdutoForm, ImagemUpload), ProdutoForm.IdProduto == 0 ? "Produto cadastrado com sucesso." : "Produto atualizado com sucesso.");

    // Cadastro pelo componente em modo AJAX (ex.: Novo/Editar da pesquisa de
    // produto na Compra). Mesmas regras do OnPost, mas responde JSON
    // para a tela de origem nao ser recarregada.
    public Task<IActionResult> OnPostSalvarAjax() =>
        ExecutarJson(async () =>
        {
            var produto = await _produto.Salvar(ProdutoForm, ImagemUpload);
            return new
            {
                sucesso = true,
                id = produto.IdProduto,
                nome = produto.DsProduto,
                unidade = produto.SgUnidade,
                custo = produto.VlCusto,
                venda = produto.VlVenda,
                estoque = produto.QtSaldo
            };
        });

    public Task<IActionResult> OnPostExcluir(int id) =>
        Executar(() => _produto.Excluir(id), "Produto excluído com sucesso.");

    // ============================================================
    // Cadastro rapido de Marca via AJAX (sem sair da tela de Produtos)
    // ============================================================
    public class MarcaRapidoDto
    {
        public int IdMarca { get; set; }
        public string NmMarca { get; set; } = string.Empty;
    }

    public Task<IActionResult> OnPostMarcaRapido([FromBody] MarcaRapidoDto dto) =>
        ExecutarJson(async () =>
        {
            var marca = await _marca.Salvar(new Marca { IdMarca = dto?.IdMarca ?? 0, NmMarca = dto?.NmMarca ?? string.Empty, FlAtivo = true });
            return new { sucesso = true, id = marca.IdMarca, nome = marca.NmMarca };
        });

    // ============================================================
    // Cadastro rapido de Categoria via AJAX (mesmo padrao da Marca)
    // ============================================================
    public class CategoriaRapidoDto
    {
        public int IdCategoria { get; set; }
        public string NmCategoria { get; set; } = string.Empty;
    }

    public Task<IActionResult> OnPostCategoriaRapido([FromBody] CategoriaRapidoDto dto) =>
        ExecutarJson(async () =>
        {
            var categoria = await _categoria.Salvar(new Categoria { IdCategoria = dto?.IdCategoria ?? 0, NmCategoria = dto?.NmCategoria ?? string.Empty, FlAtivo = true });
            return new { sucesso = true, id = categoria.IdCategoria, nome = categoria.NmCategoria };
        });

    // ============================================================
    // Busca de produtos via AJAX (filtros da pagina)
    // Pesquisa por: codigo/ID ou nome/descricao
    // ============================================================
    public async Task<IActionResult> OnGetBuscarProdutos(int? id, string? termo, bool incluirInativos = false)
    {
        var lista = await _produto.Buscar(id, termo, incluirInativos);
        return new JsonResult(lista.Select(p => new
        {
            id = p.IdProduto, registro = Module.Registro(p),
            nome = p.DsProduto,
            idMarca = p.IdMarca,
            marca = p.Marca?.NmMarca,
            idCategoria = p.IdCategoria,
            categoria = p.Categoria?.NmCategoria,
            unidade = p.SgUnidade,
            custo = p.VlCusto,
            vendaMin = p.VlVendaMinimo,
            venda = p.VlVenda,
            comissao = p.PcComissao,
            desconto = p.VlDesconto,
            estoque = p.QtSaldo,
            descricao = p.DsDescricao,
            imagem = p.DsImagem,
            ativo = p.FlAtivo
        }));
    }
}
