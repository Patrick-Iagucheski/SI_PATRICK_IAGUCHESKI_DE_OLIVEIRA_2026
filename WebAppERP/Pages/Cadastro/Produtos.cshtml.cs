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
    public List<EstMarca> Marcas { get; set; } = [];
    public List<EstCategoria> Categorias { get; set; } = [];

    [BindProperty]
    public EstProduto ProdutoForm { get; set; } = new();

    // Arquivo de imagem enviado pelo formulario (opcional)
    [BindProperty]
    public IFormFile? ImagemUpload { get; set; }

    private static readonly string[] ExtensoesPermitidas = [".jpg", ".jpeg", ".png", ".webp", ".gif"];
    private const long TamanhoMaxImagem = 500L * 1024 * 1024; // 500 MB

    public async Task OnGetAsync()
    {
        // Por padrao a listagem mostra apenas ativos (checkbox de inativos desmarcada).
        Produtos = await _db.Produtos
            .Include(p => p.Marca)
            .Include(p => p.Categoria)
            .Where(p => p.FlAtivo)
            .OrderBy(p => p.DsProduto)
            .ToListAsync();

        // Apenas marcas e categorias ativas nos dropdowns.
        Marcas = await _db.Marcas.Where(m => m.FlAtivo).OrderBy(m => m.NmMarca).ToListAsync();
        Categorias = await _db.Categorias.Where(c => c.FlAtivo).OrderBy(c => c.NmCategoria).ToListAsync();
    }

    private static string? Truncar(string? valor, int max) =>
        string.IsNullOrEmpty(valor) ? valor : (valor.Length > max ? valor[..max] : valor);

    public async Task<IActionResult> OnPostAsync()
    {
        // ===== Validacoes amigaveis =====
        if (string.IsNullOrWhiteSpace(ProdutoForm.DsProduto))
            return ComErro("Informe o nome do produto.");

        if (ProdutoForm.PcComissao is < 0 or > 999.99m)
            return ComErro("Percentual de comissão inválido.");

        if (NegativoInvalido(ProdutoForm.VlCusto) || NegativoInvalido(ProdutoForm.VlVendaMinimo) ||
            NegativoInvalido(ProdutoForm.VlVenda) || NegativoInvalido(ProdutoForm.VlDesconto) ||
            NegativoInvalido(ProdutoForm.QtSaldo))
            return ComErro("Valores monetários e estoque não podem ser negativos.");

        // Marca vem do dropdown (idMarca). Valida se a marca informada existe.
        if (ProdutoForm.IdMarca.HasValue && !await _db.Marcas.AnyAsync(m => m.IdMarca == ProdutoForm.IdMarca.Value))
            return ComErro("Marca inválida.");

        // Categoria vem do dropdown (idCategoria). Valida se a categoria existe.
        if (ProdutoForm.IdCategoria.HasValue && !await _db.Categorias.AnyAsync(c => c.IdCategoria == ProdutoForm.IdCategoria.Value))
            return ComErro("Categoria inválida.");

        // ===== Truncamento defensivo (evita erro de banco) =====
        ProdutoForm.DsProduto = Truncar(ProdutoForm.DsProduto, 300) ?? string.Empty;
        ProdutoForm.SgUnidade = Truncar(ProdutoForm.SgUnidade, 10);
        ProdutoForm.DsDescricao = Truncar(ProdutoForm.DsDescricao, 500);

        // ===== Upload da imagem (opcional) =====
        string? novaImagem = null;
        if (ImagemUpload is { Length: > 0 })
        {
            var ext = Path.GetExtension(ImagemUpload.FileName).ToLowerInvariant();
            if (!ExtensoesPermitidas.Contains(ext))
                return ComErro("Formato de imagem inválido. Use JPG, PNG, WEBP ou GIF.");
            if (ImagemUpload.Length > TamanhoMaxImagem)
                return ComErro("A imagem deve ter no máximo 5 MB.");

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
                existing.IdMarca = ProdutoForm.IdMarca;
                existing.IdCategoria = ProdutoForm.IdCategoria;
                existing.SgUnidade = ProdutoForm.SgUnidade;
                existing.VlCusto = ProdutoForm.VlCusto;
                existing.VlVendaMinimo = ProdutoForm.VlVendaMinimo;
                existing.VlVenda = ProdutoForm.VlVenda;
                existing.PcComissao = ProdutoForm.PcComissao;
                existing.VlDesconto = ProdutoForm.VlDesconto;
                existing.QtSaldo = ProdutoForm.QtSaldo;
                existing.DsDescricao = ProdutoForm.DsDescricao;
                existing.FlAtivo = ProdutoForm.FlAtivo;
                if (novaImagem != null)
                {
                    // Enviou uma nova imagem: apaga a antiga e usa a nova.
                    RemoverImagemAntiga(existing.DsImagem);
                    existing.DsImagem = novaImagem;
                }
                else if (string.IsNullOrEmpty(ProdutoForm.DsImagem))
                {
                    // Usuario removeu a imagem (campo oculto limpo): apaga a atual.
                    RemoverImagemAntiga(existing.DsImagem);
                    existing.DsImagem = null;
                }
                // Sem novo upload e sem remocao: mantem a imagem atual (existing.DsImagem).
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
            TempData["Erro"] = "Não é possível excluir: existe venda vinculada a este produto.";
            return RedirectToPage();
        }

        // tbFisNFeItens.idProduto tambem referencia o produto, mas nao tem DbSet -
        // esse caso cai no catch abaixo.
        var imagem = produto.DsImagem;
        try
        {
            _db.Produtos.Remove(produto);
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            TempData["Erro"] = "Não é possível excluir: existem registros vinculados a este produto.";
            return RedirectToPage();
        }

        // A imagem so sai do disco depois que a exclusao foi confirmada no banco,
        // senao um delete recusado deixaria o produto sem o arquivo.
        RemoverImagemAntiga(imagem);
        TempData["Sucesso"] = "Produto excluído com sucesso.";
        return RedirectToPage();
    }

    // ============================================================
    // Cadastro rapido de Marca via AJAX (sem sair da tela de Produtos)
    // ============================================================
    public class MarcaRapidoDto
    {
        public string NmMarca { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnPostMarcaRapidoAsync([FromBody] MarcaRapidoDto dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.NmMarca))
            return new JsonResult(new { sucesso = false, mensagem = "Informe o nome da marca." });

        var nome = dto.NmMarca.Trim();

        bool existe = await _db.Marcas.AnyAsync(m => m.NmMarca == nome);
        if (existe)
            return new JsonResult(new { sucesso = false, mensagem = "Já existe uma marca com esse nome." });

        var marca = new EstMarca
        {
            NmMarca = nome,
            FlAtivo = true,
            DtCriacao = DateTime.Now
        };

        _db.Marcas.Add(marca);
        await _db.SaveChangesAsync();

        return new JsonResult(new { sucesso = true, id = marca.IdMarca, nome = marca.NmMarca });
    }

    // ============================================================
    // Busca de produtos via AJAX (filtros da pagina)
    // Pesquisa por: codigo/ID ou nome/descricao
    // ============================================================
    public async Task<IActionResult> OnGetBuscarProdutosAsync(int? id, string? termo, bool incluirInativos = false)
    {
        var query = _db.Produtos.Include(p => p.Marca).Include(p => p.Categoria).AsQueryable();

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
                (p.Marca != null && p.Marca.NmMarca.Contains(t)) ||
                (p.Categoria != null && p.Categoria.NmCategoria.Contains(t)) ||
                (p.DsDescricao != null && p.DsDescricao.Contains(t)));
        }

        var lista = await query
            .OrderBy(p => p.DsProduto)
            .Take(100)
            .Select(p => new
            {
                id = p.IdProduto,
                nome = p.DsProduto,
                idMarca = p.IdMarca,
                marca = p.Marca != null ? p.Marca.NmMarca : null,
                idCategoria = p.IdCategoria,
                categoria = p.Categoria != null ? p.Categoria.NmCategoria : null,
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
