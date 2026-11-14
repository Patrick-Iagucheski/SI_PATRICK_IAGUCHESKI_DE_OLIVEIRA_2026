using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;
using WebAppERP.Infrastructure;

namespace WebAppERP.Business.Estoque;

// Regras de negocio do produto (incluindo a imagem gravada em wwwroot/uploads).
// Usada pela tela de Produtos e pelo componente de cadastro de produto
// (pesquisa de produto da Compra e da Venda).
public class ProdutoBusiness
{
    private readonly AppDbContext _db;
    private readonly ArquivoService _arquivo;
    private readonly MarcaBusiness _marca;
    private readonly CategoriaBusiness _categoria;
    private readonly UnidadeMedidaBusiness _unidade;

    // Mesmo limite de upload configurado no Program.cs.
    private const long TamanhoMaxImagem = 500L * 1024 * 1024; // 500 MB
    private const string PastaImagem = "uploads";

    public ProdutoBusiness(AppDbContext db, ArquivoService arquivo, MarcaBusiness marca, CategoriaBusiness categoria,
                           UnidadeMedidaBusiness unidade)
    {
        _db = db;
        _arquivo = arquivo;
        _marca = marca;
        _categoria = categoria;
        _unidade = unidade;
    }

    // ============================================================
    // Consultas
    // ============================================================

    // Produtos ativos (listagem inicial da tela).
    public async Task<List<Produto>> Listar() =>
        await _db.Produtos
            .Include(p => p.Marca)
            .Include(p => p.Categoria)
            .Include(p => p.UnidadeMedida)
            .Where(p => p.FlAtivo)
            .OrderBy(p => p.DsProduto)
            .ToListAsync();

    // Pesquisa da tela: por codigo ou por nome, marca, categoria ou descricao.
    // Sem "inativos" marcado traz so os ativos; marcado, so os inativos.
    public async Task<List<Produto>> Buscar(int? id, string? termo, bool somenteInativos)
    {
        var query = _db.Produtos.Include(p => p.Marca).Include(p => p.Categoria).Include(p => p.UnidadeMedida)
                                .Where(p => p.FlAtivo != somenteInativos);

        if (id.HasValue)
            query = query.Where(p => p.IdProduto == id.Value);

        if (!string.IsNullOrWhiteSpace(termo))
        {
            query = query.Where(Module.MontarFiltroTexto<Produto>(termo,
                p => p.DsProduto, p => p.Marca!.NmMarca, p => p.Categoria!.NmCategoria, p => p.DsDescricao));
        }

        return await query.OrderBy(p => p.DsProduto).Take(100).ToListAsync();
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
        registro.IdUnidadeMedida = produto.IdUnidadeMedida;
        registro.VlVenda = produto.VlVenda;
        registro.PcComissao = produto.PcComissao;
        // Custo e estoque nao vem da tela: so a entrada pela compra (custo medio
        // e saldo) e a venda (saldo) mexem neles. Produto novo comeca sem estoque.
        registro.QtSaldo ??= 0;
        registro.DsDescricao = produto.DsDescricao;
        registro.FlAtivo = produto.FlAtivo;

        // A imagem so vai para o disco depois de todas as validacoes.
        var imagemAntiga = registro.DsImagem;
        var removerAntiga = false;
        string? novaImagem = null;
        if (imagem is { Length: > 0 })
        {
            novaImagem = await _arquivo.Gravar(imagem, PastaImagem);
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
            _arquivo.Remover(novaImagem, PastaImagem);
            throw;
        }

        if (removerAntiga && imagemAntiga != registro.DsImagem)
            _arquivo.Remover(imagemAntiga, PastaImagem);

        // Sigla da unidade para a tela que chamou (ex.: unidade do item da Compra).
        await _db.Entry(registro).Reference(r => r.UnidadeMedida).LoadAsync();
        return registro;
    }

    private async Task Validar(Produto p, IFormFile? imagem)
    {
        p.DsProduto = p.DsProduto?.Trim() ?? string.Empty;
        p.DsDescricao = string.IsNullOrWhiteSpace(p.DsDescricao) ? null : p.DsDescricao.Trim();

        if (p.DsProduto.Length == 0)
            throw new ValidacaoException("Informe o nome do produto.");
        if (p.DsProduto.Length > 100)
            throw new ValidacaoException("O nome do produto deve ter no máximo 100 caracteres.");
        // Mesmo nome (sem diferenciar maiusculas nem acentos)
        var repetido = await _db.Produtos
            .Where(x => x.IdProduto != p.IdProduto
                     && EF.Functions.Collate(x.DsProduto, Module.CollationSemAcento) == p.DsProduto)
            .Select(x => x.DsProduto)
            .FirstOrDefaultAsync();
        if (repetido != null)
            throw new ValidacaoException($"Já existe o produto \"{repetido}\".", "Utilize a pesquisa para encontrá-lo.");
        if (p.DsDescricao?.Length > 500)
            throw new ValidacaoException("A descrição deve ter no máximo 500 caracteres.");

        if (p.PcComissao is < 0 or > 100)
            throw new ValidacaoException("Percentual de comissão inválido.", "Informe um percentual entre 0 e 100.");

        if (Module.EhNegativo(p.VlVenda))
            throw new ValidacaoException("O valor de venda não pode ser negativo.");

        // Marca e categoria obrigatorias (vem da pesquisa do campo; valida se existem).
        if (p.IdCategoria is not > 0)
            throw new ValidacaoException("Selecione a categoria do produto.");
        if (!await _categoria.Existe(p.IdCategoria.Value))
            throw new ValidacaoException("Categoria inválida.");
        if (p.IdMarca is not > 0)
            throw new ValidacaoException("Selecione a marca do produto.");
        if (!await _marca.Existe(p.IdMarca.Value))
            throw new ValidacaoException("Marca inválida.");

        // Unidade de medida opcional; quando informada, do cadastro e ativa, ou a
        // que o produto ja tinha (inativada depois nao trava o cadastro).
        if (p.IdUnidadeMedida is int idUnidade && !await _unidade.ExisteAtiva(idUnidade)
            && !await _db.Produtos.AnyAsync(x => x.IdProduto == p.IdProduto && x.IdUnidadeMedida == idUnidade))
            throw new ValidacaoException("Unidade de medida inválida.");

        _arquivo.ValidarImagem(imagem, TamanhoMaxImagem, "A imagem");
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
        if (await _db.Movimentacoes.AnyAsync(m => m.IdProduto == id))
            throw new ValidacaoException("Não é possível excluir: existe movimentação de estoque deste produto.", dica);

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
        _arquivo.Remover(imagem, PastaImagem);
    }
}
