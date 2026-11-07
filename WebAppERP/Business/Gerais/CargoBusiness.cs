using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;

namespace WebAppERP.Business.Gerais;

// Regras de negocio do cargo. Usada pela tela de Cargos e pela pesquisa de
// cargo do cadastro de Funcionarios: as duas passam pelas mesmas validacoes.
public class CargoBusiness
{
    private readonly AppDbContext _db;

    public CargoBusiness(AppDbContext db) => _db = db;

    // ============================================================
    // Consultas
    // ============================================================

    public async Task<List<Cargo>> Listar() =>
        await _db.Cargos
            .Where(c => c.FlAtivo)
            .OrderBy(c => c.NmCargo)
            .ToListAsync();

    // Pesquisa da tela: por codigo ou nome/descricao. Sem "inativos" marcado
    // traz so os ativos; marcado, so os inativos.
    public async Task<List<Cargo>> Buscar(int? id, string? termo, bool somenteInativos)
    {
        var query = _db.Cargos.Where(c => c.FlAtivo != somenteInativos);

        if (id.HasValue)
            query = query.Where(c => c.IdCargo == id.Value);

        if (!string.IsNullOrWhiteSpace(termo))
        {
            query = query.Where(Module.MontarFiltroTexto<Cargo>(termo, c => c.NmCargo, c => c.DsCargo));
        }

        return await query.OrderBy(c => c.NmCargo).Take(100).ToListAsync();
    }

    // ============================================================
    // Gravacao
    // ============================================================

    // Inclui (IdCargo = 0) ou altera o cargo e devolve o registro gravado.
    public async Task<Cargo> Salvar(Cargo cargo)
    {
        var nome = cargo.NmCargo?.Trim() ?? string.Empty;
        var descricao = string.IsNullOrWhiteSpace(cargo.DsCargo) ? null : cargo.DsCargo.Trim();

        if (nome.Length == 0)
            throw new ValidacaoException("O nome do cargo é obrigatório.");
        if (nome.Length > 100)
            throw new ValidacaoException("O nome do cargo deve ter no máximo 100 caracteres.");
        if (descricao?.Length > 300)
            throw new ValidacaoException("A descrição do cargo deve ter no máximo 300 caracteres.");
        if (cargo.VlComissaoPadrao is < 0 or > 999.99m)
            throw new ValidacaoException("Comissão padrão inválida.", "Informe um percentual entre 0 e 999,99.");

        // Nome unico (respeita a constraint UQ_tbGerCargos_nmCargo)
        if (await _db.Cargos.AnyAsync(c => c.NmCargo == nome && c.IdCargo != cargo.IdCargo))
            throw new ValidacaoException($"Já existe um cargo chamado \"{nome}\".");

        Cargo registro;
        if (cargo.IdCargo == 0)
        {
            registro = new Cargo();
            _db.Cargos.Add(registro);
        }
        else
        {
            registro = await _db.Cargos.FindAsync(cargo.IdCargo)
                ?? throw new ValidacaoException("Cargo não encontrado.", "Ele pode ter sido excluído por outra pessoa.");
        }

        registro.NmCargo = nome;
        registro.DsCargo = descricao;
        registro.VlComissaoPadrao = cargo.VlComissaoPadrao;
        registro.FlAtivo = cargo.FlAtivo;

        await _db.SaveChangesAsync();
        return registro;
    }

    public async Task Excluir(int id)
    {
        var cargo = await _db.Cargos.FindAsync(id);
        if (cargo == null)
            return;

        // Nao deixa excluir um cargo que esta em uso por algum funcionario.
        if (await _db.Funcionarios.AnyAsync(f => f.IdCargo == id))
            throw new ValidacaoException("Não é possível excluir: existe funcionário vinculado a este cargo.",
                                         "Inative o cargo em vez de excluí-lo.");

        _db.Cargos.Remove(cargo);
        await _db.SaveChangesAsync();
    }
}
