using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;

namespace WebAppERP.Business.Gerais;

// Regras de negocio da categoria de servico. Usada pela tela de Categorias de
// Servico, pela pesquisa e cadastro rapido do campo de categoria do servico e
// pela lista do componente de servico.
public class CategoriaServicoBusiness
{
    private readonly AppDbContext _db;

    public CategoriaServicoBusiness(AppDbContext db) => _db = db;

    // ============================================================
    // Consultas
    // ============================================================

    public async Task<List<CategoriaServico>> Listar() =>
        await _db.CategoriasServico
            .Where(c => c.FlAtivo)
            .OrderBy(c => c.NmCategoria)
            .ToListAsync();

    // Pesquisa: por codigo ou nome. Sem "inativas" marcado traz so as ativas;
    // marcado, so as inativas.
    public async Task<List<CategoriaServico>> Buscar(int? id, string? termo, bool somenteInativas)
    {
        var query = _db.CategoriasServico.Where(c => c.FlAtivo != somenteInativas);

        if (id.HasValue)
            query = query.Where(c => c.IdCategoriaServico == id.Value);

        if (!string.IsNullOrWhiteSpace(termo))
            query = query.Where(Module.MontarFiltroTexto<CategoriaServico>(termo, c => c.NmCategoria));

        return await query.OrderBy(c => c.NmCategoria).Take(100).ToListAsync();
    }

    public async Task<bool> ExisteAtiva(int id) =>
        await _db.CategoriasServico.AnyAsync(c => c.IdCategoriaServico == id && c.FlAtivo);

    // ============================================================
    // Gravacao
    // ============================================================

    // Inclui (IdCategoriaServico = 0) ou altera a categoria e devolve o registro gravado.
    public async Task<CategoriaServico> Salvar(CategoriaServico categoria)
    {
        var nome = categoria.NmCategoria?.Trim() ?? string.Empty;

        if (nome.Length == 0)
            throw new ValidacaoException("O nome da categoria é obrigatório.");
        if (nome.Length > 100)
            throw new ValidacaoException("O nome da categoria deve ter no máximo 100 caracteres.");

        // Nome unico (sem diferenciar maiusculas nem acentos)
        if (await _db.CategoriasServico.AnyAsync(c => c.IdCategoriaServico != categoria.IdCategoriaServico
                && EF.Functions.Collate(c.NmCategoria, Module.CollationSemAcento) == nome))
            throw new ValidacaoException($"Já existe uma categoria de serviço chamada \"{nome}\".");

        CategoriaServico registro;
        if (categoria.IdCategoriaServico == 0)
        {
            registro = new CategoriaServico();
            _db.CategoriasServico.Add(registro);
        }
        else
        {
            registro = await _db.CategoriasServico.FindAsync(categoria.IdCategoriaServico)
                ?? throw new ValidacaoException("Categoria não encontrada.", "Ela pode ter sido excluída por outra pessoa.");
        }

        registro.NmCategoria = nome;
        registro.FlAtivo = categoria.FlAtivo;

        await _db.SaveChangesAsync();
        return registro;
    }

    public async Task Excluir(int id)
    {
        var categoria = await _db.CategoriasServico.FindAsync(id);
        if (categoria == null)
            return;

        if (await _db.Servicos.AnyAsync(s => s.IdCategoriaServico == id))
            throw new ValidacaoException("Não é possível excluir: existe serviço vinculado a esta categoria.",
                                         "Inative a categoria em vez de excluí-la.");

        try
        {
            _db.CategoriasServico.Remove(categoria);
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new ValidacaoException("Não é possível excluir: existem registros vinculados a esta categoria.",
                                         "Inative a categoria em vez de excluí-la.");
        }
    }
}
