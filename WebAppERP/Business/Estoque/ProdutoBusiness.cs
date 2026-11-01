using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;

namespace WebAppERP.Business.Estoque;

// Regras de negocio do produto (incluindo a imagem gravada em wwwroot/uploads).
// Usada pela tela de Produtos, pelo modal de cadastro de produto da Compra e
// pela pesquisa de produto da Compra.
public class ProdutoBusiness
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly MarcaBusiness _marca;
    private readonly CategoriaBusiness _categoria;

    private static readonly string[] ExtensoesPermitidas = [".jpg", ".jpeg", ".png", ".webp", ".gif"];

    // Mesmo limite de upload configurado no Program.cs.
    private const long TamanhoMaxImagem = 500L * 1024 * 1024; // 500 MB

    public ProdutoBusiness(AppDbContext db, IWebHostEnvironment env, MarcaBusiness marca, CategoriaBusiness categoria)
    {
        _db = db;
        _env = env;
        _marca = marca;
        _categoria = categoria;
    }

    // ============================================================
    // Consultas
    // ============================================================

    // Produtos ativos (listagem inicial da tela).
    public async Task<List<Produto>> Listar() =>
        await _db.Produtos
            .Include(p => p.Marca)
            .Include(p => p.Categoria)
            .Where(p => p.FlAtivo)
            .OrderBy(p => p.DsProduto)
            .ToListAsync();

    // Pesquisa da tela: por codigo ou por nome, marca, categoria ou descricao.
    // Sem "inativos" marcado traz so os ativos; marcado, so os inativos.
    public async Task<List<Produto>> Buscar(int? id, string? termo, bool somenteInativos)
    {
        var query = _db.Produtos.Include(p => p.Marca).Include(p => p.Categoria).Where(p => p.FlAtivo != somenteInativos);

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

        return await query.OrderBy(p => p.DsProduto).Take(100).ToListAsync();
    }

    // Pesquisa rapida (pesquisa de produto da Compra): so ativos, por codigo ou nome.
    public async Task<List<Produto>> Pesquisar(int? id, string? termo)
    {
        var query = _db.Produtos.Where(p => p.FlAtivo);

        if (id.HasValue)
            query = query.Where(p => p.IdProduto == id.Value);
        else if (!string.IsNullOrWhiteSpace(termo))
        {
            var t = termo.Trim();
            query = query.Where(p => p.DsProduto.Contains(t));
        }

        return await query.OrderBy(p => p.DsProduto).Take(50).ToListAsync();
    }

    // ============================================================
    // Gravacao
    // ============================================================

    // Inclui (IdProduto = 0) ou altera o produto e devolve o registro gravado.
    // imagem: arquivo enviado (opcional). Na alteracao, DsImagem vazio sem
    // arquivo novo significa que o usuario removeu a imagem atual.
    public async Task<Produto> Salvar(Produto produto, IFormFile? imagem)
    {
        await Validar(produto, imagem);

        Produto registro;
        if (produto.IdProduto == 0)
        {
            registro = new Produto();
            _db.Produtos.Add(registro);
        }
        else
        {
            registro = await _db.Produtos.FindAsync(produto.IdProduto)
                ?? throw new ValidacaoException("Produto não encontrado.", "Ele pode ter sido excluído por outra pessoa.");
        }

        registro.DsProduto = produto.DsProduto;
        registro.IdMarca = produto.IdMarca;
        registro.IdCategoria = produto.IdCategoria;
        registro.SgUnidade = produto.SgUnidade;
        registro.VlCusto = produto.VlCusto;
        registro.VlVendaMinimo = produto.VlVendaMinimo;
        registro.VlVenda = produto.VlVenda;
        registro.PcComissao = produto.PcComissao;
        registro.VlDesconto = produto.VlDesconto;
        registro.QtSaldo = produto.QtSaldo;
        registro.DsDescricao = produto.DsDescricao;
        registro.FlAtivo = produto.FlAtivo;

        // A imagem so vai para o disco depois de todas as validacoes.
        var imagemAntiga = registro.DsImagem;
        var removerAntiga = false;
        string? novaImagem = null;
        if (imagem is { Length: > 0 })
        {
            novaImagem = await GravarImagem(imagem);
            registro.DsImagem = novaImagem;
            removerAntiga = true;
        }
        else if (produto.IdProduto != 0 && string.IsNullOrEmpty(produto.DsImagem))
        {
            // Usuario removeu a imagem (campo oculto limpo).
            registro.DsImagem = null;
            removerAntiga = true;
        }
        // Sem novo upload e sem remocao: mantem a imagem atual.

        try
        {
            await _db.SaveChangesAsync();
        }
        catch
        {
            // Banco recusou: a imagem nova nao fica perdida no disco.
            RemoverImagem(novaImagem);
            throw;
        }

        if (removerAntiga && imagemAntiga != registro.DsImagem)
            RemoverImagem(imagemAntiga);

        return registro;
    }

    private async Task Validar(Produto p, IFormFile? imagem)
    {
        p.DsProduto = p.DsProduto?.Trim() ?? string.Empty;
        p.SgUnidade = string.IsNullOrWhiteSpace(p.SgUnidade) ? null : p.SgUnidade.Trim();
        p.DsDescricao = string.IsNullOrWhiteSpace(p.DsDescricao) ? null : p.DsDescricao.Trim();

        if (p.DsProduto.Length == 0)
            throw new ValidacaoException("Informe o nome do produto.");
        if (p.DsProduto.Length > 300)
            throw new ValidacaoException("O nome do produto deve ter no máximo 300 caracteres.");
        if (p.SgUnidade?.Length > 10)
            throw new ValidacaoException("A unidade deve ter no máximo 10 caracteres.");
        if (p.DsDescricao?.Length > 500)
            throw new ValidacaoException("A descrição deve ter no máximo 500 caracteres.");

        if (p.PcComissao is < 0 or > 999.99m)
            throw new ValidacaoException("Percentual de comissão inválido.", "Informe um percentual entre 0 e 999,99.");

        if (Module.EhNegativo(p.VlCusto) || Module.EhNegativo(p.VlVendaMinimo) ||
            Module.EhNegativo(p.VlVenda) || Module.EhNegativo(p.VlDesconto) ||
            Module.EhNegativo(p.QtSaldo))
            throw new ValidacaoException("Valores monetários e estoque não podem ser negativos.");

        if (p.VlVenda.HasValue && p.VlVendaMinimo.HasValue && p.VlVenda < p.VlVendaMinimo)
            throw new ValidacaoException("O valor de venda não pode ser menor que o valor de venda mínimo.");

        // Marca e categoria vem dos dropdowns; valida se existem.
        if (p.IdMarca.HasValue && !await _marca.Existe(p.IdMarca.Value))
            throw new ValidacaoException("Marca inválida.");
        if (p.IdCategoria.HasValue && !await _categoria.Existe(p.IdCategoria.Value))
            throw new ValidacaoException("Categoria inválida.");

        if (imagem is { Length: > 0 })
        {
            var ext = Path.GetExtension(imagem.FileName).ToLowerInvariant();
            if (!ExtensoesPermitidas.Contains(ext))
                throw new ValidacaoException("Formato de imagem inválido. Use JPG, PNG, WEBP ou GIF.");
            if (imagem.Length > TamanhoMaxImagem)
                throw new ValidacaoException("A imagem deve ter no máximo 500 MB.");
        }
    }

    public async Task Excluir(int id)
    {
        var produto = await _db.Produtos.FindAsync(id);
        if (produto == null)
            return;

        const string dica = "Inative o produto em vez de excluí-lo.";

        // Filhas: tbVenVendasItens, tbCmpComprasItens e tbFisNFeItens (idProduto).
        // A nota fiscal nao tem DbSet, por isso esse caso cai no catch.
        if (await _db.VendaItens.AnyAsync(i => i.IdProduto == id))
            throw new ValidacaoException("Não é possível excluir: existe venda vinculada a este produto.", dica);
        if (await _db.CompraItens.AnyAsync(i => i.IdProduto == id))
            throw new ValidacaoException("Não é possível excluir: existe compra vinculada a este produto.", dica);

        var imagem = produto.DsImagem;
        try
        {
            _db.Produtos.Remove(produto);
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new ValidacaoException("Não é possível excluir: existem registros vinculados a este produto.", dica);
        }

        // A imagem so sai do disco depois que a exclusao foi confirmada no banco,
        // senao um delete recusado deixaria o produto sem o arquivo.
        RemoverImagem(imagem);
    }

    // ============================================================
    // Imagem (wwwroot/uploads)
    // ============================================================

    private async Task<string> GravarImagem(IFormFile imagem)
    {
        var ext = Path.GetExtension(imagem.FileName).ToLowerInvariant();
        var pasta = Path.Combine(_env.WebRootPath, "uploads");
        Directory.CreateDirectory(pasta);
        var nomeArquivo = $"{Guid.NewGuid():N}{ext}";
        using (var stream = File.Create(Path.Combine(pasta, nomeArquivo)))
            await imagem.CopyToAsync(stream);

        return $"/uploads/{nomeArquivo}";
    }

    private void RemoverImagem(string? caminhoRelativo)
    {
        if (string.IsNullOrEmpty(caminhoRelativo)) return;
        var fisico = Path.Combine(_env.WebRootPath, caminhoRelativo.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(fisico))
        {
            try { File.Delete(fisico); } catch { /* ignora falha de exclusao */ }
        }
    }
}
