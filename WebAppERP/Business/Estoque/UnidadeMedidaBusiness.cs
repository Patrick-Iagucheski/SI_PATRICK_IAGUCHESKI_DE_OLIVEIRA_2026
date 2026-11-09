using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;

namespace WebAppERP.Business.Estoque;

// Regras de negocio da unidade de medida. Usada pela tela de Unidades de
// Medida, pela pesquisa e cadastro rapido do campo de unidade do produto e
// pela lista do componente de produto.
public class UnidadeMedidaBusiness
{
    private readonly AppDbContext _db;

    public UnidadeMedidaBusiness(AppDbContext db) => _db = db;

    // ============================================================
    // Consultas
    // ============================================================

    public async Task<List<UnidadeMedida>> Listar() =>
        await _db.UnidadesMedida
            .Where(u => u.FlAtivo)
            .OrderBy(u => u.SgUnidade)
            .ToListAsync();

    // Pesquisa: por codigo, sigla ou descricao. Sem "inativas" marcado traz so
    // as ativas; marcado, so as inativas.
    public async Task<List<UnidadeMedida>> Buscar(int? id, string? termo, bool somenteInativas)
    {
        var query = _db.UnidadesMedida.Where(u => u.FlAtivo != somenteInativas);

        if (id.HasValue)
            query = query.Where(u => u.IdUnidadeMedida == id.Value);

        if (!string.IsNullOrWhiteSpace(termo))
            query = query.Where(Module.MontarFiltroTexto<UnidadeMedida>(termo, u => u.SgUnidade, u => u.DsUnidade));

        return await query.OrderBy(u => u.SgUnidade).Take(100).ToListAsync();
    }

    public async Task<bool> ExisteAtiva(int id) =>
        await _db.UnidadesMedida.AnyAsync(u => u.IdUnidadeMedida == id && u.FlAtivo);

    // ============================================================
    // Gravacao
    // ============================================================

    // Inclui (IdUnidadeMedida = 0) ou altera a unidade e devolve o registro gravado.
    public async Task<UnidadeMedida> Salvar(UnidadeMedida unidade)
    {
        var sigla = unidade.SgUnidade?.Trim().ToUpper() ?? string.Empty;
        var descricao = unidade.DsUnidade?.Trim() ?? string.Empty;

        if (sigla.Length == 0)
            throw new ValidacaoException("Informe a sigla da unidade.", "Exemplo: UN, KG, L.");
        if (sigla.Length > 10)
            throw new ValidacaoException("A sigla deve ter no máximo 10 caracteres.");
        if (descricao.Length == 0)
            throw new ValidacaoException("Informe a descrição da unidade.", "Exemplo: Unidade, Quilograma.");
        if (descricao.Length > 60)
            throw new ValidacaoException("A descrição deve ter no máximo 60 caracteres.");

        // Sigla unica (respeita a constraint UQ_tbEstUnidadesMedida_sgUnidade)
        if (await _db.UnidadesMedida.AnyAsync(u => u.SgUnidade == sigla && u.IdUnidadeMedida != unidade.IdUnidadeMedida))
            throw new ValidacaoException($"Já existe a unidade de medida \"{sigla}\".");

        UnidadeMedida registro;
        if (unidade.IdUnidadeMedida == 0)
        {
            registro = new UnidadeMedida();
            _db.UnidadesMedida.Add(registro);
        }
        else
        {
            registro = await _db.UnidadesMedida.FindAsync(unidade.IdUnidadeMedida)
                ?? throw new ValidacaoException("Unidade de medida não encontrada.", "Ela pode ter sido excluída por outra pessoa.");
        }

        registro.SgUnidade = sigla;
        registro.DsUnidade = descricao;
        registro.FlAtivo = unidade.FlAtivo;

        await _db.SaveChangesAsync();
        return registro;
    }

    public async Task Excluir(int id)
    {
        var unidade = await _db.UnidadesMedida.FindAsync(id);
        if (unidade == null)
            return;

        if (await _db.Produtos.AnyAsync(p => p.IdUnidadeMedida == id))
            throw new ValidacaoException("Não é possível excluir: existe produto com esta unidade de medida.",
                                         "Inative a unidade em vez de excluí-la.");

        try
        {
            _db.UnidadesMedida.Remove(unidade);
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new ValidacaoException("Não é possível excluir: existem registros vinculados a esta unidade de medida.",
                                         "Inative a unidade em vez de excluí-la.");
        }
    }
}
