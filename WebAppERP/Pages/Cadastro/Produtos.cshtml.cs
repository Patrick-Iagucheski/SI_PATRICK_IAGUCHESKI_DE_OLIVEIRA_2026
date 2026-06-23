using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;
using WebAppERP.Models;

namespace WebAppERP.Pages.Cadastro;

public class ProdutosModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public ProdutosModel(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public List<EstProduto> Produtos { get; set; } = [];

    [BindProperty]
    public EstProduto ProdutoForm { get; set; } = new();

    // Arquivo de imagem enviado pelo formulario (opcional)
    [BindProperty]
    public IFormFile? ImagemUpload { get; set; }

    private static readonly string[] ExtensoesPermitidas = [".jpg", ".jpeg", ".png", ".webp", ".gif"];
    private const long TamanhoMaxImagem = 5 * 1024 * 1024; // 5 MB

    public async Task OnGetAsync()
    {
        // Por padrao a listagem mostra apenas ativos (checkbox de inativos desmarcada).
        Produtos = await _db.Produtos
            .Where(p => p.FlAtivo)
            .OrderBy(p => p.DsProduto)
            .ToListAsync();
    }

    private static string? Truncar(string? valor, int max) =>
        string.IsNullOrEmpty(valor) ? valor : (valor.Length > max ? valor[..max] : valor);

    public async Task<IActionResult> OnPostAsync()
    {
        // ===== Validacoes amigaveis =====
        if (string.IsNullOrWhiteSpace(ProdutoForm.DsProduto))
            return ComErro("Informe o nome do produto.");

        if (ProdutoForm.PcComissao is < 0 or > 999.99m)
            return ComErro("Percentual de comissao invalido.");

        if (NegativoInvalido(ProdutoForm.VlCusto) || NegativoInvalido(ProdutoForm.VlVendaMinimo) ||
            NegativoInvalido(ProdutoForm.VlVenda) || NegativoInvalido(ProdutoForm.VlDesconto) ||
            NegativoInvalido(ProdutoForm.QtSaldo))
            return ComErro("Valores monetarios e estoque nao podem ser negativos.");

        // ===== Truncamento defensivo (evita erro de banco) =====
        ProdutoForm.DsProduto = Truncar(ProdutoForm.DsProduto, 300) ?? string.Empty;
        ProdutoForm.NmMarca = Truncar(ProdutoForm.NmMarca, 60);
        ProdutoForm.DsCategoria = Truncar(ProdutoForm.DsCategoria, 60);
        ProdutoForm.SgUnidade = Truncar(ProdutoForm.SgUnidade, 10);
        ProdutoForm.DsDescricao = Truncar(ProdutoForm.DsDescricao, 500);

        // ===== Upload da imagem (opcional) =====
        string? novaImagem = null;
        if (ImagemUpload is { Length: > 0 })
        {
            var ext = Path.GetExtension(ImagemUpload.FileName).ToLowerInvariant();
            if (!ExtensoesPermitidas.Contains(ext))
                return ComErro("Formato de imagem invalido. Use JPG, PNG, WEBP ou GIF.");
            if (ImagemUpload.Length > TamanhoMaxImagem)
                return ComErro("A imagem deve ter no maximo 5 MB.");

            var pasta = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(pasta);
            var nomeArquivo = $"{Guid.NewGuid():N}{ext}";
            var caminhoFisico = Path.Combine(pasta, nomeArquivo);
            using (var stream = System.IO.File.Create(caminhoFisico))
                await ImagemUpload.CopyToAsync(stream);

            novaImagem = $"/uploads/{nomeArquivo}";
        }

        if (ProdutoForm.IdProduto == 0)
        {
            if (novaImagem != null) ProdutoForm.DsImagem = novaImagem;
            _db.Produtos.Add(ProdutoForm);
            TempData["Sucesso"] = "Produto cadastrado com sucesso.";
        }
        else
        {
            var existing = await _db.Produtos.FindAsync(ProdutoForm.IdProduto);
            if (existing != null)
            {
                existing.DsProduto = ProdutoForm.DsProduto;
                existing.NmMarca = ProdutoForm.NmMarca;
                existing.DsCategoria = ProdutoForm.DsCategoria;
                existing.SgUnidade = ProdutoForm.SgUnidade;
                existing.VlCusto = ProdutoForm.VlCusto;
                existing.VlVendaMinimo = ProdutoForm.VlVendaMinimo;
                existing.VlVenda = ProdutoForm.VlVenda;
                existing.PcComissao = ProdutoForm.PcComissao;
                existing.VlDesconto = ProdutoForm.VlDesconto;
                existing.QtSaldo = ProdutoForm.QtSaldo;
                existing.DsDescricao = ProdutoForm.DsDescricao;
                existing.FlAtivo = ProdutoForm.FlAtivo;
                // So troca a imagem se uma nova foi enviada; caso contrario mantem a atual.
                if (novaImagem != null)
                {
                    RemoverImagemAntiga(existing.DsImagem);
                    existing.DsImagem = novaImagem;
                }
                TempData["Sucesso"] = "Produto atualizado com sucesso.";
            }
        }

        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    private static bool NegativoInvalido(decimal? v) => v.HasValue && v.Value < 0;

    private IActionResult ComErro(string mensagem)
    {
        TempData["Erro"] = mensagem;
        return RedirectToPage();
    }

    private void RemoverImagemAntiga(string? caminhoRelativo)
    {
        if (string.IsNullOrEmpty(caminhoRelativo)) return;
        var fisico = Path.Combine(_env.WebRootPath, caminhoRelativo.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (System.IO.File.Exists(fisico))
        {
            try { System.IO.File.Delete(fisico); } catch { /* ignora falha de exclusao */ }
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var produto = await _db.Produtos.FindAsync(id);
        if (produto == null)
            return RedirectToPage();

        // Nao deixa excluir produto vinculado a alguma venda.
        bool emUso = await _db.VendaItens.AnyAsync(i => i.IdProduto == id);
        if (emUso)
        {
            TempData["Erro"] = "Nao e possivel excluir: existe venda vinculada a este produto.";
            return RedirectToPage();
        }

        RemoverImagemAntiga(produto.DsImagem);
        _db.Produtos.Remove(produto);
        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Produto excluido com sucesso.";
        return RedirectToPage();
    }

    // ============================================================
    // Busca de produtos via AJAX (filtros da pagina)
    // Pesquisa por: codigo/ID ou nome/descricao
    // ============================================================
    public async Task<IActionResult> OnGetBuscarProdutosAsync(int? id, string? termo, bool incluirInativos = false)
    {
        var query = _db.Produtos.AsQueryable();

        // Checkbox desmarcada => apenas ativos. Marcada => apenas inativos.
        if (incluirInativos)
            query = query.Where(p => !p.FlAtivo);
        else
            query = query.Where(p => p.FlAtivo);

        if (id.HasValue)
            query = query.Where(p => p.IdProduto == id.Value);

        if (!string.IsNullOrWhiteSpace(termo))
        {
            var t = termo.Trim();
            query = query.Where(p =>
                p.DsProduto.Contains(t) ||
                (p.NmMarca != null && p.NmMarca.Contains(t)) ||
                (p.DsCategoria != null && p.DsCategoria.Contains(t)) ||
                (p.DsDescricao != null && p.DsDescricao.Contains(t)));
        }

        var lista = await query
            .OrderBy(p => p.DsProduto)
            .Take(100)
            .Select(p => new
            {
                id = p.IdProduto,
                nome = p.DsProduto,
                marca = p.NmMarca,
                categoria = p.DsCategoria,
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
            })
            .ToListAsync();

        return new JsonResult(lista);
    }
}
