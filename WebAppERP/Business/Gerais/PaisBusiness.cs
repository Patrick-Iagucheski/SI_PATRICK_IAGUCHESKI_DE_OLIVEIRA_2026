using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;

namespace WebAppERP.Business.Gerais;

// Regras de negocio do pais. Usada pela tela de Paises e pelo cadastro rapido
// de pais do campo de cidade (Localidades).
public class PaisBusiness
{
    private readonly AppDbContext _db;

    public PaisBusiness(AppDbContext db) => _db = db;

    // ============================================================
    // Consultas
    // ============================================================

    public async Task<List<Pais>> Listar() =>
        await _db.Paises.OrderBy(p => p.NmPais).ToListAsync();

    // Lista suspensa: por nome (sem acento) ou sigla.
    public async Task<List<Pais>> Buscar(string? termo)
    {
        var query = _db.Paises.AsQueryable();
        if (!string.IsNullOrWhiteSpace(termo))
        {
            var padrao = Module.MontarLikeContem(termo);
            var t = termo.Trim();
            query = query.Where(p => EF.Functions.Like(EF.Functions.Collate(p.NmPais, Module.CollationSemAcento), padrao)
                                  || p.SgPais == t);
        }
        return await query.OrderBy(p => p.NmPais).Take(50).ToListAsync();
    }

    // ============================================================
    // Gravacao
    // ============================================================

    // Inclui (IdPais = 0) ou altera o pais e devolve o registro gravado.
    public async Task<Pais> Salvar(Pais pais)
    {
        var nome = pais.NmPais?.Trim() ?? string.Empty;
        var sigla = pais.SgPais?.Trim().ToUpper() ?? string.Empty;
        var ddi = string.IsNullOrWhiteSpace(pais.NrDDI) ? null : Module.SomenteNumeros(pais.NrDDI);
        var moeda = string.IsNullOrWhiteSpace(pais.DsMoeda) ? null : pais.DsMoeda.Trim();

        if (nome.Length == 0)
            throw new ValidacaoException("Informe o nome do país.");
        if (nome.Length > 100)
            throw new ValidacaoException("O nome do país deve ter no máximo 100 caracteres.");
        if (sigla.Length == 0)
            throw new ValidacaoException("Informe a sigla do país.", "Exemplo: BR.");
        if (sigla.Length > 5)
            throw new ValidacaoException("A sigla do país deve ter no máximo 5 caracteres.");
        if (ddi?.Length > 5)
            throw new ValidacaoException("O DDI deve ter no máximo 5 números.");
        if (moeda?.Length > 50)
            throw new ValidacaoException("A moeda deve ter no máximo 50 caracteres.");

        // Mesma sigla ou mesmo nome (sem diferenciar maiusculas nem acentos)
        var repetido = await _db.Paises
            .Where(p => p.IdPais != pais.IdPais
                     && (p.SgPais == sigla || EF.Functions.Collate(p.NmPais, Module.CollationSemAcento) == nome))
            .Select(p => p.NmPais + " / " + p.SgPais)
            .FirstOrDefaultAsync();
        if (repetido != null)
            throw new ValidacaoException($"Já existe o país \"{repetido}\".", "Utilize a pesquisa para selecioná-lo.");

        Pais registro;
        if (pais.IdPais == 0)
        {
            registro = new Pais();
            _db.Paises.Add(registro);
        }
        else
        {
            registro = await _db.Paises.FindAsync(pais.IdPais)
                ?? throw new ValidacaoException("País não encontrado.", "Ele pode ter sido excluído por outra pessoa.");
        }

        registro.NmPais = nome;
        registro.SgPais = sigla;
        registro.NrDDI = ddi;
        registro.DsMoeda = moeda;

        await _db.SaveChangesAsync();
        return registro;
    }

    public async Task Excluir(int id)
    {
        var pais = await _db.Paises.FindAsync(id);
        if (pais == null)
            return;

        // Nao deixa excluir pais que ainda tem estados (FK_tbGerEstados_Pais).
        if (await _db.Estados.AnyAsync(e => e.IdPais == id))
            throw new ValidacaoException("Não é possível excluir: existe estado vinculado a este país.");

        try
        {
            _db.Paises.Remove(pais);
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new ValidacaoException("Não é possível excluir: existem registros vinculados a este país.");
        }
    }
}
