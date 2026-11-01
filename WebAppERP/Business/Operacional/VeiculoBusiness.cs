using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using WebAppERP.Data;

namespace WebAppERP.Business.Operacional;

// Regras de negocio do veiculo (usado no transporte das notas).
public class VeiculoBusiness
{
    private readonly AppDbContext _db;

    public VeiculoBusiness(AppDbContext db) => _db = db;

    // Placa antiga (ABC1234) ou Mercosul (ABC1D23).
    private static readonly Regex FormatoPlaca = new("^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$");

    // ============================================================
    // Consultas
    // ============================================================

    public async Task<List<Veiculo>> Listar() =>
        await _db.Veiculos
            .Include(v => v.Estado)
            .OrderBy(v => v.DsPlaca)
            .ToListAsync();

    // ============================================================
    // Gravacao
    // ============================================================

    // Inclui (IdVeiculo = 0) ou altera o veiculo e devolve o registro gravado.
    public async Task<Veiculo> Salvar(Veiculo veiculo)
    {
        // Placa guardada sem traco/espaco e em maiusculas.
        var placa = Regex.Replace(veiculo.DsPlaca ?? "", "[^A-Za-z0-9]", "").ToUpperInvariant();
        var antt = string.IsNullOrWhiteSpace(veiculo.NrANTT) ? null : veiculo.NrANTT.Trim();

        if (placa.Length == 0)
            throw new ValidacaoException("Informe a placa do veículo.");
        if (!FormatoPlaca.IsMatch(placa))
            throw new ValidacaoException("Placa inválida.", "Use o formato ABC1234 ou o Mercosul ABC1D23.");
        if (antt?.Length > 20)
            throw new ValidacaoException("O registro ANTT deve ter no máximo 20 caracteres.");
        if (veiculo.IdEstado.HasValue && !await _db.Estados.AnyAsync(e => e.IdEstado == veiculo.IdEstado))
            throw new ValidacaoException("Estado (UF) inválido.");

        var repetida = await _db.Veiculos.AnyAsync(v => v.DsPlaca == placa && v.IdVeiculo != veiculo.IdVeiculo);
        if (repetida)
            throw new ValidacaoException($"Já existe um veículo com a placa {placa}.");

        Veiculo registro;
        if (veiculo.IdVeiculo == 0)
        {
            registro = new Veiculo();
            _db.Veiculos.Add(registro);
        }
        else
        {
            registro = await _db.Veiculos.FindAsync(veiculo.IdVeiculo)
                ?? throw new ValidacaoException("Veículo não encontrado.", "Ele pode ter sido excluído por outra pessoa.");
        }

        registro.DsPlaca = placa;
        registro.IdEstado = veiculo.IdEstado;
        registro.NrANTT = antt;

        await _db.SaveChangesAsync();
        return registro;
    }

    public async Task Excluir(int id)
    {
        var veiculo = await _db.Veiculos.FindAsync(id);
        if (veiculo == null)
            return;

        // A unica filha e tbFisNFe.idVeiculo, que nao tem DbSet -
        // por isso a checagem fica a cargo do catch de DbUpdateException.
        try
        {
            _db.Veiculos.Remove(veiculo);
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new ValidacaoException("Não é possível excluir: existe nota fiscal vinculada a este veículo.");
        }
    }
}
