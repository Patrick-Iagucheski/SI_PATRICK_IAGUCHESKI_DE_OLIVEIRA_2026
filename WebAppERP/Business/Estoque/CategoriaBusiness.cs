using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;

namespace WebAppERP.Business.Estoque;

// Regras de negocio da categoria. Usada pela tela de Categorias, pelo "+" de
// categoria no cadastro de produto e pela lista do componente de produto.
public class CategoriaBusiness
{
    private readonly AppDbContext _db;

    public CategoriaBusiness(AppDbContext db) => _db = db;

    // ============================================================
    // Consultas
    // ============================================================

    public async Task<List<Categoria>> Listar() =>
        await _db.Categorias
            .Where(c => c.FlAtivo)
            .OrderBy(c => c.NmCategoria)
            .ToListAsync();

    // Pesquisa da tela: por codigo ou nome. Sem "inativas" marcado traz so as
    // ativas; marcado, so as inativas.
    public async Task<List<Categoria>> Buscar(int? id, string? termo, bool somenteInativas)
    {
        var query = _db.Categorias.Where(c => c.FlAtivo != somenteInativas);

        if (id.HasValue)
            query = query.Where(c => c.IdCategoria == id.Value);

        if (!string.IsNullOrWhiteSpace(termo))
        {
            query = query.Where(Module.MontarFiltroTexto<Categoria>(termo, c => c.NmCategoria));
        }

        return await query.OrderBy(c => c.NmCategoria).Take(100).ToListAsync();
    }

    public async Task<bool> Existe(int id) => await _db.Categorias.AnyAsync(c => c.IdCategoria == id);

    // ============================================================
    // Gravacao
    // ============================================================

    // Inclui (IdCategoria = 0) ou altera a categoria e devolve o registro gravado.
    public async Task<Categoria> Salvar(Categoria categoria)
    {
        var nome = categoria.NmCategoria?.Trim() ?? string.Empty;

        if (nome.Length == 0)
            throw new ValidacaoException("O nome da categoria é obrigatório.");
        if (nome.Length > 100)
            throw new ValidacaoException("O nome da categoria deve ter no máximo 100 caracteres.");

        // Nome unico (respeita a constraint UQ_tbEstCategorias_nmCategoria)
        if (await _db.Categorias.AnyAsync(c => c.NmCategoria == nome && c.IdCategoria != categoria.IdCategoria))
            throw new ValidacaoException($"Já existe uma categoria chamada \"{nome}\".");

        Categoria registro;
        if (categoria.IdCategoria == 0)
        {
            registro = new Categoria { DtCriacao = DateTime.Now };
            _db.Categorias.Add(registro);
        }
        else
        {
            registro = await _db.Categorias.FindAsync(categoria.IdCategoria)
                ?? throw new ValidacaoException("Categoria não encontrada.", "Ela pode ter sido excluída por outra pessoa.");
            registro.DtEdicao = DateTime.Now;
        }

        registro.NmCategoria = nome;
        registro.FlAtivo = categoria.FlAtivo;

        await _db.SaveChangesAsync();
        return registro;
    }

    public async Task Excluir(int id)
    {
        var categoria = await _db.Categorias.FindAsync(id);
        if (categoria == null)
            return;

        // A FK FK_tbEstProdutos_Categoria impede excluir categoria em uso.
        if (await _db.Produtos.AnyAsync(p => p.IdCategoria == id))
            throw new ValidacaoException("Não é possível excluir: existe produto vinculado a esta categoria.",
                                         "Inative a categoria em vez de excluí-la.");

        try
        {
            _db.Categorias.Remove(categoria);
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new ValidacaoException("Não é possível excluir: existem registros vinculados a esta categoria.",
                                         "Inative a categoria em vez de excluí-la.");
        }
    }
}
