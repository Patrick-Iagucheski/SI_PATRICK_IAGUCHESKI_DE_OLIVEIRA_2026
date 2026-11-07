using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;

namespace WebAppERP.Business.Gerais;

// Regras de negocio do estado. Usada pela tela de Estados, pelo cadastro rapido
// de estado do campo de cidade (Localidades) e pelas listas de estados
// (Cidades, Veiculos).
public class EstadoBusiness
{
    private readonly AppDbContext _db;

    public EstadoBusiness(AppDbContext db) => _db = db;

    // ============================================================
    // Consultas
    // ============================================================

    public async Task<List<Estado>> Listar() =>
        await _db.Estados
            .Include(e => e.Pais)
            .OrderBy(e => e.NmEstado)
            .ToListAsync();

    // Pesquisa: por nome (sem acento) ou UF.
    public async Task<List<Estado>> Buscar(int? id, string? termo)
    {
        var query = _db.Estados.Include(e => e.Pais).AsQueryable();
        if (id.HasValue)
            query = query.Where(e => e.IdEstado == id.Value);
        if (!string.IsNullOrWhiteSpace(termo))
        {
            query = query.Where(Module.MontarFiltroTexto<Estado>(termo, e => e.NmEstado, e => e.SgUF));
        }
        return await query.OrderBy(e => e.NmEstado).Take(50).ToListAsync();
    }

    // ============================================================
    // Gravacao
    // ============================================================

    // Inclui (IdEstado = 0) ou altera o estado e devolve o registro gravado.
    public async Task<Estado> Salvar(Estado estado)
    {
        var nome = estado.NmEstado?.Trim() ?? string.Empty;
        var uf = estado.SgUF?.Trim().ToUpper() ?? string.Empty;

        if (nome.Length == 0)
            throw new ValidacaoException("Informe o nome do estado.");
        if (nome.Length > 100)
            throw new ValidacaoException("O nome do estado deve ter no máximo 100 caracteres.");
        if (uf.Length != 2)
            throw new ValidacaoException("Informe a UF com 2 letras.", "Exemplo: PR.");
        if (estado.IdPais <= 0 || !await _db.Paises.AnyAsync(p => p.IdPais == estado.IdPais))
            throw new ValidacaoException("Selecione o país do estado.");

        // No mesmo pais: mesma UF ou mesmo nome (sem diferenciar maiusculas nem acentos)
        var repetido = await _db.Estados
            .Where(e => e.IdEstado != estado.IdEstado && e.IdPais == estado.IdPais
                     && (e.SgUF == uf || EF.Functions.Collate(e.NmEstado, Module.CollationSemAcento) == nome))
            .Select(e => e.NmEstado + " / " + e.SgUF)
            .FirstOrDefaultAsync();
        if (repetido != null)
            throw new ValidacaoException($"Já existe o estado \"{repetido}\" neste país.", "Utilize a pesquisa para selecioná-lo.");

        Estado registro;
        if (estado.IdEstado == 0)
        {
            registro = new Estado { DtCriacao = DateTime.Now };
            _db.Estados.Add(registro);
        }
        else
        {
            registro = await _db.Estados.FindAsync(estado.IdEstado)
                ?? throw new ValidacaoException("Estado não encontrado.", "Ele pode ter sido excluído por outra pessoa.");
            registro.DtEdicao = DateTime.Now;
        }

        registro.NmEstado = nome;
        registro.SgUF = uf;
        registro.IdPais = estado.IdPais;

        await _db.SaveChangesAsync();
        return registro;
    }

    public async Task Excluir(int id)
    {
        var estado = await _db.Estados.FindAsync(id);
        if (estado == null)
            return;

        // Filhas: tbGerCidades.idEstado e tbOpeVeiculos.idEstado.
        if (await _db.Cidades.AnyAsync(c => c.IdEstado == id))
            throw new ValidacaoException("Não é possível excluir: existe cidade vinculada a este estado.");
        if (await _db.Veiculos.AnyAsync(v => v.IdEstado == id))
            throw new ValidacaoException("Não é possível excluir: existe veículo vinculado a este estado.");

        try
        {
            _db.Estados.Remove(estado);
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new ValidacaoException("Não é possível excluir: existem registros vinculados a este estado.");
        }
    }
}
