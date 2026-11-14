using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;

namespace WebAppERP.Business.Gerais;

// Regras de negocio da cidade. Usada pela tela de Cidades, pelo campo de cidade
// de todas as telas com endereco (CampoCidade + Localidades) e pelo seu
// cadastro rapido.
public class CidadeBusiness
{
    private readonly AppDbContext _db;

    public CidadeBusiness(AppDbContext db) => _db = db;

    // ============================================================
    // Consultas
    // ============================================================

    public async Task<List<Cidade>> Listar() =>
        await _db.Cidades
            .Include(c => c.Estado)
            .OrderBy(c => c.NmCidade)
            .ToListAsync();

    // Pesquisa: por nome (sem acento) ou UF. Com id, traz so aquela
    // cidade (edicao de registro que so tem o idCidade).
    public async Task<List<Cidade>> Buscar(int? id, string? termo, string? uf = null)
    {
        var query = _db.Cidades.Include(c => c.Estado).AsQueryable();
        if (id.HasValue)
            query = query.Where(c => c.IdCidade == id.Value);
        if (!string.IsNullOrWhiteSpace(termo))
            query = query.Where(Module.MontarFiltroTexto<Cidade>(termo, c => c.NmCidade, c => c.Estado!.SgUF));
        if (!string.IsNullOrWhiteSpace(uf))
        {
            var sigla = uf.Trim().ToUpper();
            query = query.Where(c => c.Estado != null && c.Estado.SgUF == sigla);
        }
        return await query.OrderBy(c => c.NmCidade).Take(50).ToListAsync();
    }

    // "Cidade / UF" para exibir no campo de cidade.
    public async Task<string?> GetNomeComUf(int id) =>
        await _db.Cidades
            .Where(c => c.IdCidade == id)
            .Select(c => c.NmCidade + (c.Estado != null ? " / " + c.Estado.SgUF : ""))
            .FirstOrDefaultAsync();

    // ============================================================
    // Gravacao
    // ============================================================

    // Inclui (IdCidade = 0) ou altera a cidade e devolve o registro gravado,
    // com o Estado carregado (o cadastro rapido devolve a UF para a tela).
    public async Task<Cidade> Salvar(Cidade cidade)
    {
        var nome = cidade.NmCidade?.Trim() ?? string.Empty;

        if (nome.Length == 0)
            throw new ValidacaoException("Informe o nome da cidade.");
        if (nome.Length > 60)
            throw new ValidacaoException("O nome da cidade deve ter no máximo 60 caracteres.");

        var estado = cidade.IdEstado > 0 ? await _db.Estados.FindAsync(cidade.IdEstado) : null;
        if (estado == null)
            throw new ValidacaoException("Selecione o estado da cidade.");

        // No mesmo estado: mesmo nome (sem diferenciar maiusculas nem acentos)
        var repetida = await _db.Cidades
            .Where(c => c.IdCidade != cidade.IdCidade && c.IdEstado == cidade.IdEstado
                     && EF.Functions.Collate(c.NmCidade, Module.CollationSemAcento) == nome)
            .Select(c => c.NmCidade)
            .FirstOrDefaultAsync();
        if (repetida != null)
            throw new ValidacaoException($"Já existe a cidade \"{repetida}\" neste estado.", "Utilize a pesquisa para selecioná-la.");

        Cidade registro;
        if (cidade.IdCidade == 0)
        {
            registro = new Cidade();
            _db.Cidades.Add(registro);
        }
        else
        {
            registro = await _db.Cidades.FindAsync(cidade.IdCidade)
                ?? throw new ValidacaoException("Cidade não encontrada.", "Ela pode ter sido excluída por outra pessoa.");
        }

        registro.NmCidade = nome;
        registro.IdEstado = estado.IdEstado;
        registro.Estado = estado;

        await _db.SaveChangesAsync();
        return registro;
    }

    public async Task Excluir(int id)
    {
        var cidade = await _db.Cidades.FindAsync(id);
        if (cidade == null)
            return;

        // Filhas: clientes, fornecedores, funcionarios e transportadores (idCidade).
        if (await _db.Clientes.AnyAsync(c => c.IdCidade == id))
            throw new ValidacaoException("Não é possível excluir: existe cliente vinculado a esta cidade.");
        if (await _db.Fornecedores.AnyAsync(f => f.IdCidade == id))
            throw new ValidacaoException("Não é possível excluir: existe fornecedor vinculado a esta cidade.");
        if (await _db.Funcionarios.AnyAsync(f => f.IdCidade == id))
            throw new ValidacaoException("Não é possível excluir: existe funcionário vinculado a esta cidade.");
        if (await _db.Transportadores.AnyAsync(t => t.IdCidade == id))
            throw new ValidacaoException("Não é possível excluir: existe transportador vinculado a esta cidade.");

        try
        {
            _db.Cidades.Remove(cidade);
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new ValidacaoException("Não é possível excluir: existem registros vinculados a esta cidade.");
        }
    }
}
