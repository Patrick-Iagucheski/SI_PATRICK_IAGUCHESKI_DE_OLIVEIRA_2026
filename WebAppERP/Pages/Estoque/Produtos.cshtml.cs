using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebAppERP.Business.Estoque;

namespace WebAppERP.Pages.Estoque;

// Tela de Produtos: so conversa com a tela. As regras (e a imagem) ficam em ProdutoBusiness.
public class ProdutosModel : PageModel
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

    public async Task<IActionResult> OnPost()
    {
        try
        {
            var novo = ProdutoForm.IdProduto == 0;
            await _produto.Salvar(ProdutoForm, ImagemUpload);
            TempData["Sucesso"] = novo ? "Produto cadastrado com sucesso." : "Produto atualizado com sucesso.";
        }
        catch (ValidacaoException ex)
        {
            TempData["Erro"] = ex.MensagemCompleta;
        }
        return RedirectToPage();
    }

    // Cadastro pelo componente em modo AJAX (ex.: botao + da pesquisa de
    // produto na Compra). Mesmas regras do OnPost, mas responde JSON
    // para a tela de origem nao ser recarregada.
    public async Task<IActionResult> OnPostSalvarAjax()
    {
        try
        {
            var produto = await _produto.Salvar(ProdutoForm, ImagemUpload);
            return new JsonResult(new
            {
                sucesso = true,
                id = produto.IdProduto,
                nome = produto.DsProduto,
                unidade = produto.SgUnidade,
                custo = produto.VlCusto
            });
        }
        catch (ValidacaoException ex)
        {
            return new JsonResult(new { sucesso = false, mensagem = ex.MensagemCompleta });
        }
    }

    public async Task<IActionResult> OnPostExcluir(int id)
    {
        try
        {
            await _produto.Excluir(id);
            TempData["Sucesso"] = "Produto excluído com sucesso.";
        }
        catch (ValidacaoException ex)
        {
            TempData["Erro"] = ex.MensagemCompleta;
        }
        return RedirectToPage();
    }

    // ============================================================
    // Cadastro rapido de Marca via AJAX (sem sair da tela de Produtos)
    // ============================================================
    public class MarcaRapidoDto
    {
        public string NmMarca { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnPostMarcaRapido([FromBody] MarcaRapidoDto dto)
    {
        try
        {
            var marca = await _marca.Salvar(new Marca { NmMarca = dto?.NmMarca ?? string.Empty, FlAtivo = true });
            return new JsonResult(new { sucesso = true, id = marca.IdMarca, nome = marca.NmMarca });
        }
        catch (ValidacaoException ex)
        {
            return new JsonResult(new { sucesso = false, mensagem = ex.MensagemCompleta });
        }
    }

    // ============================================================
    // Cadastro rapido de Categoria via AJAX (mesmo padrao da Marca)
    // ============================================================
    public class CategoriaRapidoDto
    {
        public string NmCategoria { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnPostCategoriaRapido([FromBody] CategoriaRapidoDto dto)
    {
        try
        {
            var categoria = await _categoria.Salvar(new Categoria { NmCategoria = dto?.NmCategoria ?? string.Empty, FlAtivo = true });
            return new JsonResult(new { sucesso = true, id = categoria.IdCategoria, nome = categoria.NmCategoria });
        }
        catch (ValidacaoException ex)
        {
            return new JsonResult(new { sucesso = false, mensagem = ex.MensagemCompleta });
        }
    }

    // ============================================================
    // Busca de produtos via AJAX (filtros da pagina)
    // Pesquisa por: codigo/ID ou nome/descricao
    // ============================================================
    public async Task<IActionResult> OnGetBuscarProdutos(int? id, string? termo, bool incluirInativos = false)
    {
        var lista = await _produto.Buscar(id, termo, incluirInativos);
        return new JsonResult(lista.Select(p => new
        {
            id = p.IdProduto,
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
