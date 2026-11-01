using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;

namespace WebAppERP.Business.Estoque;

// Regras de negocio da marca. Usada pela tela de Marcas, pelo "+" de marca no
// cadastro de produto e pela lista de marcas do componente de produto.
public class MarcaBusiness
{
    private readonly AppDbContext _db;

    public MarcaBusiness(AppDbContext db) => _db = db;

    // ============================================================
    // Consultas
    // ============================================================

    public async Task<List<Marca>> Listar() =>
        await _db.Marcas
            .Where(m => m.FlAtivo)
            .OrderBy(m => m.NmMarca)
            .ToListAsync();

    // Pesquisa da tela: por codigo ou nome. Sem "inativas" marcado traz so as
    // ativas; marcado, so as inativas.
    public async Task<List<Marca>> Buscar(int? id, string? termo, bool somenteInativas)
    {
        var query = _db.Marcas.Where(m => m.FlAtivo != somenteInativas);

        if (id.HasValue)
            query = query.Where(m => m.IdMarca == id.Value);

        if (!string.IsNullOrWhiteSpace(termo))
        {
            var t = termo.Trim();
            query = query.Where(m => m.NmMarca.Contains(t));
        }

        return await query.OrderBy(m => m.NmMarca).Take(100).ToListAsync();
    }

    public async Task<bool> Existe(int id) => await _db.Marcas.AnyAsync(m => m.IdMarca == id);

    // ============================================================
    // Gravacao
    // ============================================================

    // Inclui (IdMarca = 0) ou altera a marca e devolve o registro gravado.
    public async Task<Marca> Salvar(Marca marca)
    {
        var nome = marca.NmMarca?.Trim() ?? string.Empty;

        if (nome.Length == 0)
            throw new ValidacaoException("O nome da marca é obrigatório.");
        if (nome.Length > 100)
            throw new ValidacaoException("O nome da marca deve ter no máximo 100 caracteres.");

        // Nome unico (respeita a constraint UQ_tbEstMarcas_nmMarca)
        if (await _db.Marcas.AnyAsync(m => m.NmMarca == nome && m.IdMarca != marca.IdMarca))
            throw new ValidacaoException($"Já existe uma marca chamada \"{nome}\".");

        Marca registro;
        if (marca.IdMarca == 0)
        {
            registro = new Marca { DtCriacao = DateTime.Now };
            _db.Marcas.Add(registro);
        }
        else
        {
            registro = await _db.Marcas.FindAsync(marca.IdMarca)
                ?? throw new ValidacaoException("Marca não encontrada.", "Ela pode ter sido excluída por outra pessoa.");
            registro.DtEdicao = DateTime.Now;
        }

        registro.NmMarca = nome;
        registro.FlAtivo = marca.FlAtivo;

        await _db.SaveChangesAsync();
        return registro;
    }

    public async Task Excluir(int id)
    {
        var marca = await _db.Marcas.FindAsync(id);
        if (marca == null)
            return;

        // A FK FK_tbEstProdutos_Marca impede excluir marca em uso.
        if (await _db.Produtos.AnyAsync(p => p.IdMarca == id))
            throw new ValidacaoException("Não é possível excluir: existe produto vinculado a esta marca.",
                                         "Inative a marca em vez de excluí-la.");

        try
        {
            _db.Marcas.Remove(marca);
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new ValidacaoException("Não é possível excluir: existem registros vinculados a esta marca.",
                                         "Inative a marca em vez de excluí-la.");
        }
    }
}
